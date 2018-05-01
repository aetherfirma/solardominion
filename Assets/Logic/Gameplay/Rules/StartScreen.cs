﻿using Logic.Maths;
using Logic.Network;
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
        private Button _start, _join;
        private InputField _field;

        public StartScreen(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (!_setup)
            {
                _ui = Object.Instantiate(_referee.StartScreen, _referee.UiCanvas);

                _start = _ui.Find("Start Game").gameObject.GetComponent<Button>();
                _join = _ui.Find("Join Game").gameObject.GetComponent<Button>();
                _field = _ui.Find("Game UUID").gameObject.GetComponent<InputField>();
                
                _start.onClick.AddListener(StartGame);
                _join.onClick.AddListener(JoinGame);
                
                _setup = true;
            }
        }

        private void StartGame()
        {
            var wwwForm = new WWWForm();
            wwwForm.AddField("players", "2");
            wwwForm.AddField("scenario", _referee.Scenario.Name);
            var www = UnityWebRequest.Post(_referee.ServerUrl + "/game/create", wwwForm);
            www.SendWebRequest();

            while (!www.isDone) ;
 
            if(www.isNetworkError) {
                _referee.FlashMessage("There was a network error creating a game\n"+www.error);
            } 
            else if (www.isHttpError)
            {
                _referee.FlashMessage("There was a server error (" + www.responseCode +  ") creating a game\n"+www.error);
            }
            else
            {
                var response = GameResponse.FromJson(www.downloadHandler.text);
                _referee.SetGameState(response);
                _referee.GameUuid = response.id;
                _referee.PlayerUuid = response.players[0];
                _referee.FlashMessage("You have joined game " + _referee.GameUuid);
                _referee.Phase = GamePhase.PlayerSelection;
                _referee.Rng = new WellRng(response.seed);
                Destroy();
            }
        }

        private void JoinGame()
        {
            var wwwForm = new WWWForm();
            var www = UnityWebRequest.Post(_referee.ServerUrl + "/game/" + _field.text + "/join", wwwForm);
            _field.text = "";
            www.SendWebRequest();

            while (!www.isDone) ;
 
            if(www.isNetworkError) {
                _referee.FlashMessage("There was a network error joining a game\n"+www.error);
            } 
            else if (www.isHttpError)
            {
                _referee.FlashMessage("There was a server error (" + www.responseCode +  ") joining a game\n"+www.error);
            }
            else
            {
                var response = JoinResponse.FromJson(www.downloadHandler.text);
                _referee.SetGameState(response.game);
                _referee.GameUuid = response.game.id;
                _referee.PlayerUuid = response.player_id;
                _referee.FlashMessage("You have joined game " + _referee.GameUuid);
                _referee.Phase = GamePhase.PlayerSelection;
                _referee.Rng = new WellRng(response.game.seed);
                Destroy();
            }
        }

        private void Destroy()
        {
            Object.Destroy(_ui.gameObject);
        }
    }
}