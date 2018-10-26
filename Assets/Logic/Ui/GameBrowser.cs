using System.Collections;
using System.Collections.Generic;
using Logic.Gameplay.Rules;
using Logic.Network;
using Logic.Utilities;
using UnityEngine;

namespace Logic.Ui
{
    public class GameBrowser : MonoBehaviour
    {
        public RectTransform ScrollPanel;
        public RectTransform GamePanel;
        public Referee Referee;
        private GameListResponse _gameIds;
        private Dictionary<string, GameInfo> _games;
        private List<RectTransform> _gameDisplays;

        private IEnumerator UpdateGameInfo()
        {
            for (;;)
            {
                SimpleRequest.Get(
                    Referee.ServerUrl + "/games",
                    www =>
                    {
                        _gameIds = GameListResponse.FromJson(www.downloadHandler.text);
                        foreach (var gameId in _gameIds.games)
                        {
                            SimpleRequest.Get(
                                Referee.ServerUrl + "/game/" + gameId,
                                www1 => Referee.SetGameState(GameResponse.FromJson(www.downloadHandler.text)),
                                www1 => Referee.FlashMessage("There was a server error (" + www.responseCode + ")\n" + www.error),
                                www1 => Referee.FlashMessage("There was a network error\n" + www.error)
                            );
                        }
                    },
                    www => Referee.FlashMessage("There was a server error (" + www.responseCode + ")\n" + www.error),
                    www => Referee.FlashMessage("There was a network error\n" + www.error)
                );
                yield return new WaitForSecondsRealtime(Referee.UpdateInterval);
            }
        }
    }
}