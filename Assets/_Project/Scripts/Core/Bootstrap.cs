using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// Lives in Boot.unity. Resets services, moves the state machine to Menu and loads the next scene.
    /// </summary>
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] private string _nextScene = "MainMenu";
        [SerializeField] private int _targetFrameRate = 144;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Services.Reset();

            Application.targetFrameRate = _targetFrameRate;

            // Touching the properties creates the services in a known order.
            var state = Services.State;
            state.TryTransitionTo(GameState.Menu);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_nextScene)) SceneManager.LoadScene(_nextScene);
        }

        private void Update() => Services.State.Update(Services.Clock.DeltaTime);
    }
}
