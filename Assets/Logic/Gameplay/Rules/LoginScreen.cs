using Logic.Network;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Logic.Gameplay.Rules
{
    public class LoginScreen
    {
        private Referee _referee;
        private RectTransform _loginScreen;

        private TMP_InputField _username, _password;

        public LoginScreen(Referee referee)
        {
            _referee = referee;
        }

        public void Update()
        {
            if (_loginScreen == null)
            {
                _loginScreen = Object.Instantiate(_referee.LoginScreen, _referee.UiCanvas);
                _username = _loginScreen.Find("Username").GetComponent<TMP_InputField>();
                _password = _loginScreen.Find("Password").GetComponent<TMP_InputField>();

                _username.onSubmit.AddListener(Login);
                _password.onSubmit.AddListener(Login);
                _loginScreen.Find("Start Game").GetComponent<Button>().onClick.AddListener(Login);
                _loginScreen.Find("Quit").GetComponent<Button>().onClick.AddListener(Application.Quit);
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_username.isFocused)
                {
                    EventSystem.current.SetSelectedGameObject(_password.gameObject);
                    _password.caretPosition = _password.text.Length;
                }

                if (_password.isFocused)
                {
                    EventSystem.current.SetSelectedGameObject(_username.gameObject);
                    _username.caretPosition = _username.text.Length;
                }
            }
        }

        private void Login(string value)
        {
            Login();
        }

        private void Login()
        {
            var username = _username.text;
            var password = _password.text;
            
            SimpleRequest.Get(
                string.Format("{0}/user/test", _referee.ServerUrl), username, password,
                www =>
                {
                    _referee.Phase = GamePhase.MainMenu;
                    _referee.Username = username;
                    _referee.Password = password;
                    Object.Destroy(_loginScreen.gameObject);
                    _loginScreen = null;
                },
                www =>
                {
                    _username.text = "";
                    _password.text = "";
                    _referee.FlashMessage("Your username and/or password are incorrect, please try again");
                    EventSystem.current.SetSelectedGameObject(_username.gameObject);
                },
                www =>
                {
                    _referee.FlashMessage("There has been a network error, please try again later");
                }
            );
            Debug.Log("Tried to log in as " + _username.text);
        }
    }
}