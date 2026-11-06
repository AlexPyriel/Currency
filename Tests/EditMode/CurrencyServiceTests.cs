using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GDF.Currency.Config;
using GDF.Currency.Data;
using GDF.Currency.Messages;
using LocalDataStorage;
using MessagePipe;
using NUnit.Framework;
using UnityEngine;

namespace GDF.Currency.Tests.EditMode
{
    public sealed class CurrencyServiceTests
    {
        private CurrencyRepository _repo;
        private InMemoryStorage _storage;
        private IPublisher<CurrencyChangedMessage> _changedPublisher;
        private IPublisher<CurrencySpendRejectedMessage> _rejectedPublisher;
        private List<CurrencyChangedMessage> _changed;
        private List<CurrencySpendRejectedMessage> _rejected;
        private IDisposable _subscriptions;
        private Internal.CurrencyService _service;

        [SetUp]
        public void SetUp()
        {
            _repo = ScriptableObject.CreateInstance<CurrencyRepository>();
            SeedRepository(_repo, "gold");
            SeedRepository(_repo, "gems");

            BuiltinContainerBuilder pipe = new BuiltinContainerBuilder();
            pipe.AddMessagePipe();
            pipe.AddMessageBroker<CurrencyChangedMessage>();
            pipe.AddMessageBroker<CurrencySpendRejectedMessage>();
            IServiceProvider provider = pipe.BuildServiceProvider();
            _changedPublisher = provider.GetRequiredService<IPublisher<CurrencyChangedMessage>>();
            _rejectedPublisher = provider.GetRequiredService<IPublisher<CurrencySpendRejectedMessage>>();

            _changed = new List<CurrencyChangedMessage>();
            _rejected = new List<CurrencySpendRejectedMessage>();
            _subscriptions = DisposableBag.Create(
                provider.GetRequiredService<ISubscriber<CurrencyChangedMessage>>().Subscribe(_changed.Add),
                provider.GetRequiredService<ISubscriber<CurrencySpendRejectedMessage>>().Subscribe(_rejected.Add));

            _storage = new InMemoryStorage();
            _service = CreateService();
            _service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            _subscriptions.Dispose();
            UnityEngine.Object.DestroyImmediate(_repo);
        }

        [Test]
        public void Ctor_NullPublisher_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Internal.CurrencyService(_repo, _storage, null, _rejectedPublisher));
            Assert.Throws<ArgumentNullException>(() => new Internal.CurrencyService(_repo, _storage, _changedPublisher, null));
        }

        [Test]
        public void Initialize_SeedsDefaults_AndPublishesNothing()
        {
            Assert.AreEqual(0, _service.Get("gold"));
            Assert.AreEqual(0, _service.Get("gems"));
            Assert.IsEmpty(_changed, "Initialize restores state; it is not a change.");
        }

        [Test]
        public void Add_IncreasesBalance_AndEarnedTotal_AndPublishesChange()
        {
            _service.Add("gold", 50, "level_reward");

            Assert.AreEqual(50, _service.Get("gold"));
            Assert.AreEqual(50, _service.GetEarnedTotal("gold"));
            Assert.AreEqual(1, _changed.Count);
            AssertChanged(_changed[0], "gold", 0, 50, "level_reward");
            Assert.AreEqual(50, _changed[0].Delta);
        }

        [Test]
        public void Add_NonPositiveAmount_IsIgnored()
        {
            _service.Add("gold", 0, "noop");
            _service.Add("gold", -5, "noop");

            Assert.AreEqual(0, _service.Get("gold"));
            Assert.IsEmpty(_changed);
        }

        [Test]
        public void Spend_SucceedsAndLeavesEarnedTotalUntouched_AndPublishesNegativeDelta()
        {
            _service.Add("gold", 200, "seed");
            _changed.Clear();

            bool ok = _service.Spend("gold", 150, "shop_purchase");

            Assert.IsTrue(ok);
            Assert.AreEqual(50, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"), "Earned total ignores spending.");
            Assert.AreEqual(1, _changed.Count);
            AssertChanged(_changed[0], "gold", 200, 50, "shop_purchase");
            Assert.AreEqual(-150, _changed[0].Delta);
            Assert.IsEmpty(_rejected);
        }

        [Test]
        public void Spend_InsufficientBalance_PublishesRejected_NoMutation()
        {
            _service.Add("gems", 5, "seed");
            _changed.Clear();

            bool ok = _service.Spend("gems", 1000, "revive");

            Assert.IsFalse(ok);
            Assert.AreEqual(5, _service.Get("gems"));
            Assert.IsEmpty(_changed);
            Assert.AreEqual(1, _rejected.Count);
            Assert.AreEqual("gems", _rejected[0].CurrencyId);
            Assert.AreEqual(1000, _rejected[0].Requested);
            Assert.AreEqual(5, _rejected[0].Available);
            Assert.AreEqual("revive", _rejected[0].Reason);
        }

        [Test]
        public void Spend_NonPositiveAmount_ReturnsTrue_AndPublishesNothing()
        {
            Assert.IsTrue(_service.Spend("gold", 0, "noop"));
            Assert.IsEmpty(_changed);
            Assert.IsEmpty(_rejected);
        }

        [Test]
        public void Reset_ZeroesBothCounters_AndPublishesChange()
        {
            _service.Add("gems", 10, "seed");
            _changed.Clear();

            _service.Reset("gems", "season_end");

            Assert.AreEqual(0, _service.Get("gems"));
            Assert.AreEqual(0, _service.GetEarnedTotal("gems"));
            Assert.AreEqual(1, _changed.Count);
            AssertChanged(_changed[0], "gems", 10, 0, "season_end");
        }

        [Test]
        public void Reset_ZeroBalance_PublishesNothing()
        {
            _service.Reset("gems", "season_end");

            Assert.IsEmpty(_changed);
        }

        [Test]
        public void Set_WithIncrease_GrowsEarnedByDelta_AndPublishesEachChange()
        {
            _service.Set("gold", 200, "debug");
            Assert.AreEqual(200, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"));

            _service.Set("gold", 50, "debug");
            Assert.AreEqual(50, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"), "Earned total does not shrink on Set(lower).");

            Assert.AreEqual(2, _changed.Count);
            AssertChanged(_changed[0], "gold", 0, 200, "debug");
            AssertChanged(_changed[1], "gold", 200, 50, "debug");
        }

        [Test]
        public void Set_SameValue_PublishesNothing()
        {
            _service.Set("gold", 0, "debug");

            Assert.IsEmpty(_changed);
        }

        [Test]
        public void Mutations_EmptyReason_Throw()
        {
            Assert.Throws<ArgumentException>(() => _service.Add("gold", 1, null));
            Assert.Throws<ArgumentException>(() => _service.Spend("gold", 1, string.Empty));
            Assert.Throws<ArgumentException>(() => _service.Set("gold", 1, null));
            Assert.Throws<ArgumentException>(() => _service.Reset("gold", string.Empty));
            Assert.AreEqual(0, _service.Get("gold"));
            Assert.IsEmpty(_changed);
        }

        [Test]
        public void UnknownCurrency_IsSafelyIgnored()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            Assert.AreEqual(0, _service.Get("foo"));
            _service.Add("foo", 10, "test");
            Assert.IsFalse(_service.Spend("foo", 10, "test"));

            Assert.AreEqual(0, _service.Get("foo"));
            Assert.IsEmpty(_changed);
            Assert.IsEmpty(_rejected, "Unknown ids are not insufficient funds.");
        }

        [Test]
        public void Persist_RoundTripsThroughStorage()
        {
            _service.Add("gold", 125, "seed");
            Internal.CurrencyService fresh = CreateService();
            fresh.Initialize();
            Assert.AreEqual(125, fresh.Get("gold"));
        }

        private Internal.CurrencyService CreateService()
        {
            return new Internal.CurrencyService(_repo, _storage, _changedPublisher, _rejectedPublisher);
        }

        private static void AssertChanged(CurrencyChangedMessage message, string id, int oldValue, int newValue, string reason)
        {
            Assert.AreEqual(id, message.CurrencyId);
            Assert.AreEqual(oldValue, message.OldValue);
            Assert.AreEqual(newValue, message.NewValue);
            Assert.AreEqual(reason, message.Reason);
        }

        private static void SeedRepository(CurrencyRepository repo, string id)
        {
            // Repository.TrySet wires the id; DefaultValue stays 0 (it is a private serialized field).
            repo.TrySet(id, new CurrencyConfig());
        }

        private sealed class InMemoryStorage : ILocalDataStorage
        {
            private readonly Dictionary<Type, LocalData> _store = new();
            public event Action<Type> DataSaved;
            public event Action<Type> DataDeleted;
            public event Action AllDeleted;

            public void Save<T>(T data) where T : LocalData { _store[typeof(T)] = data; DataSaved?.Invoke(typeof(T)); }
            public void Save<T>(string slot, T data) where T : LocalData => Save(data);
            public bool TryLoad<T>(out T data) where T : LocalData
            {
                if (_store.TryGetValue(typeof(T), out LocalData stored) && stored is T typed) { data = typed; return true; }
                data = null; return false;
            }
            public bool TryLoad<T>(string slot, out T data) where T : LocalData => TryLoad(out data);
            public void Delete<T>() where T : LocalData { _store.Remove(typeof(T)); DataDeleted?.Invoke(typeof(T)); }
            public void Delete<T>(string slot) where T : LocalData => Delete<T>();
            public IEnumerable<string> ListSlots<T>() where T : LocalData => _store.ContainsKey(typeof(T)) ? new[] { string.Empty } : Array.Empty<string>();
            public void DeleteAll() { _store.Clear(); AllDeleted?.Invoke(); }

            public UniTask SaveAsync<T>(T data, CancellationToken ct = default) where T : LocalData { Save(data); return UniTask.CompletedTask; }
            public UniTask SaveAsync<T>(string slot, T data, CancellationToken ct = default) where T : LocalData { Save(data); return UniTask.CompletedTask; }
            public UniTask<(bool Found, T Data)> TryLoadAsync<T>(CancellationToken ct = default) where T : LocalData => UniTask.FromResult<(bool, T)>((TryLoad(out T d), d));
            public UniTask<(bool Found, T Data)> TryLoadAsync<T>(string slot, CancellationToken ct = default) where T : LocalData => UniTask.FromResult<(bool, T)>((TryLoad(out T d), d));
            public UniTask DeleteAsync<T>(string slot = "", CancellationToken ct = default) where T : LocalData { Delete<T>(); return UniTask.CompletedTask; }
            public UniTask DeleteAllAsync(CancellationToken ct = default) { DeleteAll(); return UniTask.CompletedTask; }
        }
    }
}
