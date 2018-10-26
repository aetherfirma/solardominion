using System.Collections;
using Logic.Maths;
using Logic.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules
{
    public class JoinGameScreen
    {
        private Referee _referee;
        private RectTransform _ui;
        private Coroutine _coroutine;
        private WaitingGame[] _games;
        private bool _gamesUpdated;
        private ScrollRect _scroll;

        public JoinGameScreen(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (_ui == null)
            {
                _ui = Object.Instantiate(_referee.ServerBrowser, _referee.UiCanvas);
                _ui.Find("Back Button").gameObject.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        _referee.Phase = GamePhase.MainMenu;
                        Destroy();
                    });
                _scroll = _ui.Find("Panel/Scroll View").GetComponent<ScrollRect>();
                
                _coroutine = _referee.StartCoroutine(GetWaitingGames());
            }

            if (_gamesUpdated)
            {
                _gamesUpdated = false;

                foreach (Transform child in _scroll.content)
                {
                    Object.Destroy(child.gameObject);
                }

                var n = 0;

                foreach (var game in _games)
                {
                    var item = Object.Instantiate(_referee.ServerBrowserItem, _scroll.content);
                    item.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Format(
                        "<b>{0}<b> ({1} points)\n<size=16>Players: {2}</size>",
                        game.scenario,
                        game.points_limit,
                        string.Join(", ", game.players)
                    );
                    item.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                    {
                        JoinGame(game.id);
                    });
                    item.localPosition =  new Vector2(10, -110 * n);
                    n++;
                }
                _scroll.content.sizeDelta = new Vector2(_scroll.content.sizeDelta.x, n * 110);
            }
        }

        public IEnumerator GetWaitingGames()
        {
            while (true)
            {
                SimpleRequest.Get(
                    string.Format("{0}/games/waiting", _referee.ServerUrl),
                    www =>
                    {
                        var response = WaitingResponse.FromJson(www.downloadHandler.text);
                        _games = response.games;
                        _gamesUpdated = true;

                    },
                    www => { },
                    www => { }
                );
                
                yield return new WaitForSecondsRealtime(_referee.UpdateInterval);
            }
        }
    
        private void JoinGame(string gameId)
        {
            var wwwForm = new WWWForm();
            SimpleRequest.Post(
                _referee.ServerUrl + "/game/" + gameId + "/join", _referee.Username, _referee.Password, wwwForm,
                www =>
                {
                    var response = JoinResponse.FromJson(www.downloadHandler.text);
                    _referee.SetGameState(response.game);
                    _referee.GameUuid = response.game.id;
                    _referee.PlayerUuid = response.player_id;
                    _referee.Phase = GamePhase.PlayerSelection;
                    _referee.Rng = new WellRng(response.game.seed);
                    _referee.StartCoroutine(_referee.GameStateCoroutine());
                    Destroy();
                },
                www => _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error),
                www => _referee.FlashMessage("There was a network error creating a game\n"+www.error)
            );
        }

        private void Destroy()
        {
            Object.Destroy(_ui.gameObject);
            _ui = null;
            _referee.StopCoroutine(_coroutine);
        }
    }
}