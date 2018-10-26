using Logic.Maths;
using Logic.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules
{
    public class NewGameScreen
    {
        private Referee _referee;
        private RectTransform _ui;
        private Slider _slider;
        private TextMeshProUGUI _gameSize;
        private TextMeshProUGUI _gamePoints;

        private int PointsValue
        {
            get { return (int) (_slider.value * 50); }
        }

        public NewGameScreen(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (_ui == null)
            {
                _ui = Object.Instantiate(_referee.NewGameScreen, _referee.UiCanvas);
                _ui.Find("Start Game").GetComponent<Button>().onClick.AddListener(StartGame);
                _slider = _ui.Find("Slider").GetComponent<Slider>();
                _gameSize = _ui.Find("Game Size").GetComponent<TextMeshProUGUI>();
                _gamePoints = _ui.Find("Game Points").GetComponent<TextMeshProUGUI>();
            }

            _gamePoints.text = string.Format("{0} points", PointsValue);
            if (PointsValue < 750)
            {
                _gameSize.text = "Small Game";
            }
            else if (PointsValue < 1750)
            {
                _gameSize.text = "Medium Game";
            }
            else
            {
                _gameSize.text = "Large Game";
            }
        }

        private void StartGame()
        {
            var wwwForm = new WWWForm();
            wwwForm.AddField("players", "2");
            wwwForm.AddField("scenario", _referee.Scenario.Name);
            wwwForm.AddField("points_limit", PointsValue);

            SimpleRequest.Post(
                _referee.ServerUrl + "/game/create",
                _referee.Username, _referee.Password,
                wwwForm,
                www => {
                    var response = GameResponse.FromJson(www.downloadHandler.text);
                    _referee.SetGameState(response);
                    _referee.GameUuid = response.id;
                    _referee.PlayerUuid = response.players[0];
                    _referee.FlashMessage("You have joined game " + _referee.GameUuid);
                    _referee.Phase = GamePhase.PlayerSelection;
                    _referee.Rng = new WellRng(response.seed);
                    _referee.StartCoroutine(_referee.GameStateCoroutine());
                    Destroy();
                },
                www => { _referee.FlashMessage("There was a network error creating a game\n" + www.error); },
                www => { _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error); }
            );
        }

        private void Destroy()
        {
            Object.Destroy(_ui.gameObject);
            _ui = null;
        }
    }
}