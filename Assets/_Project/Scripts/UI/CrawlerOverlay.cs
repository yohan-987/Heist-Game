using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Shows spiders crawling over the local player's screen and lets them swat them away.
    /// Put on a full-screen UI container. The crawler prefab is any UI object with a RectTransform
    /// (an Image with a spider sprite, for example).
    /// </summary>
    public sealed class CrawlerOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private RectTransform _crawlerPrefab;
        [SerializeField] private int _seed = 7;

        private readonly CrawlerSwarmModel _model = new CrawlerSwarmModel();
        private readonly List<RectTransform> _views = new List<RectTransform>();
        private DeterministicRandom _rng;

        private void Awake()
        {
            _rng = new DeterministicRandom(_seed);
            if (_container == null) _container = transform as RectTransform;
        }

        private void OnEnable() => Services.Events.Subscribe<SpiderAttack>(OnSpiders);

        private void OnDisable() => Services.Events.Unsubscribe<SpiderAttack>(OnSpiders);

        private void OnSpiders(SpiderAttack evt)
        {
            var local = Services.Players.Local;
            if (local == null || local.PlayerId != evt.PlayerId) return;

            _model.Add(evt.Count, _rng);
            Sync();
        }

        private void Update()
        {
            if (_model.Count == 0) return;

            _model.Tick(Services.Clock.DeltaTime);

            var local = Services.Players.Local;
            if (local != null)
            {
                var input = local.Transform.GetComponentInParent<IPlayerInputSource>();
                if (input != null && input.Current.SwatPressed) _model.Swat();
            }

            Sync();
        }

        private void Sync()
        {
            if (_container == null || _crawlerPrefab == null) return;

            while (_views.Count < _model.Count) _views.Add(Instantiate(_crawlerPrefab, _container));
            while (_views.Count > _model.Count)
            {
                int last = _views.Count - 1;
                if (_views[last] != null) Destroy(_views[last].gameObject);
                _views.RemoveAt(last);
            }

            Rect rect = _container.rect;
            float minSide = Mathf.Min(rect.width, rect.height);

            for (int i = 0; i < _views.Count; i++)
            {
                var crawler = _model.Crawlers[i];
                var view = _views[i];
                var anchor = new Vector2(crawler.X, crawler.Y);

                view.anchorMin = anchor;
                view.anchorMax = anchor;
                view.pivot = new Vector2(0.5f, 0.5f);
                view.anchoredPosition = Vector2.zero;

                // Coverage is a screen-area fraction, so the side length is its square root.
                float side = Mathf.Sqrt(crawler.Coverage) * minSide;
                view.sizeDelta = new Vector2(side, side);
            }
        }
    }
}
