using Logic.Maths;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules
{
    public class StartScreen
    {
        private Referee _referee;
        private RectTransform _ui;
        private bool _setup;

        public StartScreen(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (_ui == null)
            {
                _ui = Object.Instantiate(_referee.StartScreen, _referee.UiCanvas);

                _ui.Find("Start Game").gameObject.GetComponent<Button>().onClick.AddListener(StartGame);
                _ui.Find("Join Game").gameObject.GetComponent<Button>().onClick.AddListener(JoinGame);
                _ui.Find("Quit").gameObject.GetComponent<Button>().onClick.AddListener(Application.Quit);
            }
        }

        private void StartGame()
        {
            _referee.Phase = GamePhase.NewGame;
            Destroy();
        }

        private void JoinGame()
        {
            _referee.Phase = GamePhase.JoinGame;
            Destroy();
        }

        private void Destroy()
        {
            Object.Destroy(_ui.gameObject);
            _ui = null;
        }
    }
}