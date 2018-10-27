using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules
{
    public class PauseHandler
    {
        private Referee _referee;
        private RectTransform _ui;
        public bool Paused;

        public PauseHandler(Referee referee)
        {
            _referee = referee;
        }
        
        public void Unpause()
        {
            Paused = false;
            if (_ui != null)
            {
                Object.Destroy(_ui.gameObject);
                _ui = null;                
            }
        }

        public void Update()
        {
            var escDown = Input.GetKeyDown(KeyCode.Escape);
            if (!Paused)
            {
                if (escDown) Paused = true;
                else return;
            } else if (escDown)
            {
                Unpause();
                return;
            }
            if (_ui == null)
            {
                _ui = Object.Instantiate(_referee.PauseMenu, _referee.UiCanvas);
                _ui.Find("Panel/Resume").GetComponent<Button>().onClick.AddListener(Unpause);
                _ui.Find("Panel/Quit").GetComponent<Button>().onClick.AddListener(Application.Quit);
                _ui.Find("Panel/Login Notice").GetComponent<TextMeshProUGUI>().text = _referee.Username == ""
                    ? "Not logged in"
                    : string.Format("Logged in as {0}", _referee.Username);
            }
        }
    }
}