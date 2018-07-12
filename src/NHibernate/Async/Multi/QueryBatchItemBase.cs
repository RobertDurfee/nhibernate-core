﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by AsyncGenerator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using NHibernate.Cache;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.Util;

namespace NHibernate.Multi
{
	using System.Threading.Tasks;
	using System.Threading;
	public abstract partial class QueryBatchItemBase<TResult> : IQueryBatchItem<TResult>
	{

		/// <inheritdoc />
		public async Task<IEnumerable<ISqlCommand>> GetCommandsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var yields = new List<ISqlCommand>();
			for (var index = 0; index < _queryInfos.Count; index++)
			{
				var qi = _queryInfos[index];

				if (qi.Loader.IsCacheable(qi.Parameters))
				{
					qi.IsCacheable = true;
					// Check if the results are available in the cache
					qi.Cache = Session.Factory.GetQueryCache(qi.Parameters.CacheRegion);
					qi.CacheKey = qi.Loader.GenerateQueryKey(Session, qi.Parameters);
					var resultsFromCache = await (qi.Loader.GetResultFromQueryCacheAsync(Session, qi.Parameters, qi.QuerySpaces, qi.Cache, qi.CacheKey, cancellationToken)).ConfigureAwait(false);

					if (resultsFromCache != null)
					{
						// Cached results available, skip the command for them and stores them.
						_loaderResults[index] = resultsFromCache;
						qi.IsResultFromCache = true;
						continue;
					}
				}
			yields.Add(qi.Loader.CreateSqlCommand(qi.Parameters, Session));
			}
			return yields;
		}

		/// <inheritdoc />
		public async Task<int> ProcessResultsSetAsync(DbDataReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var dialect = Session.Factory.Dialect;
			var hydratedObjects = new List<object>[_queryInfos.Count];

			var rowCount = 0;
			for (var i = 0; i < _queryInfos.Count; i++)
			{
				var queryInfo = _queryInfos[i];
				var loader = queryInfo.Loader;
				var queryParameters = queryInfo.Parameters;

				//Skip processing for items already loaded from cache
				if (queryInfo.IsResultFromCache)
				{
					continue;
				}

				var entitySpan = loader.EntityPersisters.Length;
				hydratedObjects[i] = entitySpan == 0 ? null : new List<object>(entitySpan);
				var keys = new EntityKey[entitySpan];

				var selection = queryParameters.RowSelection;
				var createSubselects = loader.IsSubselectLoadingEnabled;

				_subselectResultKeys[i] = createSubselects ? new List<EntityKey[]>() : null;
				var maxRows = Loader.Loader.HasMaxRows(selection) ? selection.MaxRows : int.MaxValue;
				var advanceSelection = !dialect.SupportsLimitOffset || !loader.UseLimit(selection, dialect);

				if (advanceSelection)
				{
					await (Loader.Loader.AdvanceAsync(reader, selection, cancellationToken)).ConfigureAwait(false);
				}

				var forcedResultTransformer = queryInfo.CacheKey?.ResultTransformer;
				if (queryParameters.HasAutoDiscoverScalarTypes)
				{
					loader.AutoDiscoverTypes(reader, queryParameters, forcedResultTransformer);
				}

				var lockModeArray = loader.GetLockModes(queryParameters.LockModes);
				var optionalObjectKey = Loader.Loader.GetOptionalObjectKey(queryParameters, Session);
				var tmpResults = new List<object>();

				for (var count = 0; count < maxRows && await (reader.ReadAsync(cancellationToken)).ConfigureAwait(false); count++)
				{
					rowCount++;

					var o =
						await (loader.GetRowFromResultSetAsync(
							reader,
							Session,
							queryParameters,
							lockModeArray,
							optionalObjectKey,
							hydratedObjects[i],
							keys,
							true,
							forcedResultTransformer
, cancellationToken						)).ConfigureAwait(false);
					if (loader.IsSubselectLoadingEnabled)
					{
						_subselectResultKeys[i].Add(keys);
						keys = new EntityKey[entitySpan]; //can't reuse in this case
					}

					tmpResults.Add(o);
				}

				_loaderResults[i] = tmpResults;

				await (reader.NextResultAsync(cancellationToken)).ConfigureAwait(false);
			}

			await (InitializeEntitiesAndCollectionsAsync(reader, hydratedObjects, cancellationToken)).ConfigureAwait(false);

			return rowCount;
		}

		/// <inheritdoc />
		public async Task ProcessResultsAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			for (var i = 0; i < _queryInfos.Count; i++)
			{
				var queryInfo = _queryInfos[i];
				if (_subselectResultKeys[i] != null)
				{
					queryInfo.Loader.CreateSubselects(_subselectResultKeys[i], queryInfo.Parameters, Session);
				}

				// Handle cache if cacheable.
				if (queryInfo.IsCacheable)
				{
					if (!queryInfo.IsResultFromCache)
					{
						await (queryInfo.Loader.PutResultInQueryCacheAsync(
							Session,
							queryInfo.Parameters,
							queryInfo.Cache,
							queryInfo.CacheKey,
							_loaderResults[i], cancellationToken)).ConfigureAwait(false);
					}

					_loaderResults[i] =
						queryInfo.Loader.TransformCacheableResults(
							queryInfo.Parameters, queryInfo.CacheKey.ResultTransformer, _loaderResults[i]);
				}
			}
			AfterLoadCallback?.Invoke(GetResults());
		}

		/// <inheritdoc />
		public async Task ExecuteNonBatchedAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			_finalResults = await (GetResultsNonBatchedAsync(cancellationToken)).ConfigureAwait(false);
			AfterLoadCallback?.Invoke(_finalResults);
		}

		protected abstract Task<IList<TResult>> GetResultsNonBatchedAsync(CancellationToken cancellationToken);

		private async Task InitializeEntitiesAndCollectionsAsync(DbDataReader reader, List<object>[] hydratedObjects, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			for (var i = 0; i < _queryInfos.Count; i++)
			{
				var queryInfo = _queryInfos[i];
				if (queryInfo.IsResultFromCache)
					continue;
				await (queryInfo.Loader.InitializeEntitiesAndCollectionsAsync(
					hydratedObjects[i], reader, Session, Session.PersistenceContext.DefaultReadOnly, cancellationToken)).ConfigureAwait(false);
			}
		}
	}
}