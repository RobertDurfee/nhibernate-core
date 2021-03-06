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
using System.Data.Common;
using NUnit.Framework;

namespace NHibernate.Test.TransactionTest
{
	using System.Threading.Tasks;
	using System.Threading;
	[TestFixture]
	public class TransactionNotificationFixtureAsync : TestCase
	{
		protected override string[] Mappings
		{
			get { return Array.Empty<string>(); }
		}

		[Test]
		public async Task CommitAsync()
		{
			var interceptor = new RecordingInterceptor();
			using (var session = Sfi.WithOptions().Interceptor(interceptor).OpenSession())
			{
				ITransaction tx = session.BeginTransaction();
				await (tx.CommitAsync());
				Assert.That(interceptor.afterTransactionBeginCalled, Is.EqualTo(1));
				Assert.That(interceptor.beforeTransactionCompletionCalled, Is.EqualTo(1));
				Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1));
			}
		}

		[Test]
		public async Task RollbackAsync()
		{
			var interceptor = new RecordingInterceptor();
			using (var session = Sfi.WithOptions().Interceptor(interceptor).OpenSession())
			{
				ITransaction tx = session.BeginTransaction();
				await (tx.RollbackAsync());
				Assert.That(interceptor.afterTransactionBeginCalled, Is.EqualTo(1));
				Assert.That(interceptor.beforeTransactionCompletionCalled, Is.EqualTo(0));
				Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1));
			}
		}


		[Theory]
		[Description("NH2128")]
		public async Task ShouldNotifyAfterTransactionAsync(bool usePrematureClose)
		{
			var interceptor = new RecordingInterceptor();
			ISession s;

			using (s = OpenSession(interceptor))
			using (s.BeginTransaction())
			{
				await (s.CreateCriteria<object>().ListAsync());

				// Call session close while still inside transaction?
				if (usePrematureClose)
					s.Close();
			}

			Assert.That(s.IsOpen, Is.False);
			Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1));
		}


		[Description("NH2128")]
		[Theory]
		public async Task ShouldNotifyAfterTransactionWithOwnConnectionAsync(bool usePrematureClose)
		{
			var interceptor = new RecordingInterceptor();
			ISession s;

			using (var ownConnection = await (Sfi.ConnectionProvider.GetConnectionAsync(CancellationToken.None)))
			{
				using (s = Sfi.WithOptions().Connection(ownConnection).Interceptor(interceptor).OpenSession())
				using (s.BeginTransaction())
				{
					await (s.CreateCriteria<object>().ListAsync());

					// Call session close while still inside transaction?
					if (usePrematureClose)
						s.Close();
				}
			}

			Assert.That(s.IsOpen, Is.False);
			Assert.That(interceptor.afterTransactionCompletionCalled, Is.EqualTo(1));
		}
	}
}
