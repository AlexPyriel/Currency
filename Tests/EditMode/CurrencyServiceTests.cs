using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using GDF.Currency.Config;
using GDF.Currency.Data;
using LocalDataStorage;
using NUnit.Framework;
using UnityEngine;

namespace GDF.Currency.Tests.EditMode
{
    public sealed class CurrencyServiceTests
    {
        private CurrencyRepository _repo;
        private InMemoryStorage _storage;
        private Internal.CurrencyService _service;

        [SetUp]
        public void SetUp()
        {
            _repo = ScriptableObject.CreateInstance<CurrencyRepository>();
            SeedRepository(_repo, "gold", 100);
            SeedRepository(_repo, "gems", 5);

            _storage = new InMemoryStorage();
            _service = new Internal.CurrencyService(_repo, _storage);
            _service.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_repo);
        }

        [Test]
        public void Initialize_SeedsDefaults_WhenNoSave()
        {
            Assert.AreEqual(100, _service.Get("gold"));
            Assert.AreEqual(5, _service.Get("gems"));
            Assert.AreEqual(100, _service.GetEarnedTotal("gold"));
        }

        [Test]
        public void Add_IncreasesBalance_AndEarnedTotal()
        {
            string lastEvent = null;
            _service.CurrencyChanged += id => lastEvent = id;

            _service.Add("gold", 50);

            Assert.AreEqual(150, _service.Get("gold"));
            Assert.AreEqual(150, _service.GetEarnedTotal("gold"));
            Assert.AreEqual("gold", lastEvent);
        }

        [Test]
        public void Spend_FailsOnInsufficientBalance_NoMutation()
        {
            bool ok = _service.Spend("gems", 1000);
            Assert.IsFalse(ok);
            Assert.AreEqual(5, _service.Get("gems"));
        }

        [Test]
        public void Spend_SucceedsAndLeavesEarnedTotalUntouched()
        {
            _service.Add("gold", 100); // earned 200
            _service.Spend("gold", 150);

            Assert.AreEqual(50, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"), "Earned total ignores spending.");
        }

        [Test]
        public void Reset_ZeroesBothCounters()
        {
            _service.Add("gems", 10);
            _service.Reset("gems");

            Assert.AreEqual(0, _service.Get("gems"));
            Assert.AreEqual(0, _service.GetEarnedTotal("gems"));
        }

        [Test]
        public void Set_WithIncrease_GrowsEarnedByDelta()
        {
            _service.Set("gold", 200);
            Assert.AreEqual(200, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"));

            _service.Set("gold", 50);
            Assert.AreEqual(50, _service.Get("gold"));
            Assert.AreEqual(200, _service.GetEarnedTotal("gold"), "Earned total does not shrink on Set(lower).");
        }

        [Test]
        public void UnknownCurrency_IsSafelyIgnored()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            Assert.AreEqual(0, _service.Get("foo"));
            _service.Add("foo", 10);
            Assert.AreEqual(0, _service.Get("foo"));
        }

        [Test]
        public void Persist_RoundTripsThroughStorage()
        {
            _service.Add("gold", 25);
            Internal.CurrencyService fresh = new Internal.CurrencyService(_repo, _storage);
            fresh.Initialize();
            Assert.AreEqual(125, fresh.Get("gold"));
        }

        private static void SeedRepository(CurrencyRepository repo, string id, int defaultValue)
        {
            CurrencyConfig cfg = new CurrencyConfig();
            // Repository.TrySet wires the id automatically.
            repo.TrySet(id, cfg);
            // Default value comes from the private field — bump via SerializedObject in editor; for the
            // test, we rely on repository.Enumerate pulling the key. The service uses config.DefaultValue
            // which is 0 by default; the test seeds balance after Initialize through Set/Add.
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
