using System.Linq;
using Logic.Gameplay.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Gameplay.Ships
{
    public class ShipCard : MonoBehaviour
    {
        public Ship Ship;
        public RectTransform SystemTransform;
        public RectTransform CompositeTransform;
        private Transform _systemDisplay, _compositeDisplay;
        private Button _lockOnTarget, _braceForImpact, _fullThrust, _onMyMark;
        private Button[] _compositeOptionAs, _compositeOptionBs;
        private TextMeshProUGUI _initiative, _speed, _thrust;
        public Sprite SystemBackground, DamagedSystemBackground;

        private readonly Color _unmColor = new Color(0.31f, 0.54f, 1f);
        private readonly Color _ip3Color = new Color(0.77f, 0.12f, 0.14f);
        private Color _factionColor;
        private int _knownInitiative;
        private int _knownSpeed;
        private int _knownThrust;
        private Order _knownOrder;
        private bool _knownUnderOrder;
        private bool _knownOrderable;
        private int[] _knownSubsystems;
        private int[] _compositeSystems;
        private Image[] _systemImageBackgrounds;
        private bool[] _knownDamage;

        private void Start()
        {
            _factionColor = Ship.Faction == Faction.UNM ? _unmColor : _ip3Color;

            transform.Find("Ship Name").GetComponent<TextMeshProUGUI>().text = Ship.Name();
            transform.Find("Faction Border").GetComponent<Image>().color = _factionColor;
            var description = transform.Find("Ship Description").GetComponent<TextMeshProUGUI>();
            description.text = string.Format(
                "{0} {1} Class\n{2}{3}", Ship.Faction.ToString(), Ship.ShipClass,
                Ship.ClassVariant == "" ? "" : Ship.ClassVariant + " ", Ship.Class);
            description.color = _factionColor;

            _systemDisplay = transform.Find("Grid");
            _compositeDisplay = transform.Find("Composite System Status");

            _lockOnTarget = transform.Find("Order Icons/Lock On Target").GetComponent<Button>();
            var colorBlock = _lockOnTarget.colors;
            colorBlock.highlightedColor = _factionColor;
            _lockOnTarget.colors = colorBlock;
            _lockOnTarget.onClick.AddListener(() => SetOrder(Order.LockOnTarget));

            _braceForImpact = transform.Find("Order Icons/Brace For Impact").GetComponent<Button>();
            colorBlock = _braceForImpact.colors;
            colorBlock.highlightedColor = _factionColor;
            _braceForImpact.colors = colorBlock;
            _braceForImpact.onClick.AddListener(() => SetOrder(Order.BraceForImpact));

            _fullThrust = transform.Find("Order Icons/Full Thrust").GetComponent<Button>();
            colorBlock = _fullThrust.colors;
            colorBlock.highlightedColor = _factionColor;
            _fullThrust.colors = colorBlock;
            _fullThrust.onClick.AddListener(() => SetOrder(Order.MilitaryThrust));

            _onMyMark = transform.Find("Order Icons/On My Mark").GetComponent<Button>();
            colorBlock = _onMyMark.colors;
            colorBlock.highlightedColor = _factionColor;
            _onMyMark.colors = colorBlock;
            _onMyMark.onClick.AddListener(() => SetOrder(Order.OnMyMark));

            transform.Find("Rating").GetComponent<TextMeshProUGUI>().text = string.Format("Rating\n{0}", Ship.Training);
            _initiative = transform.Find("Initiative").GetComponent<TextMeshProUGUI>();
            _speed = transform.Find("Speed").GetComponent<TextMeshProUGUI>();
            _thrust = transform.Find("Thrust").GetComponent<TextMeshProUGUI>();

            transform.Find("Composite System Title").GetComponent<TextMeshProUGUI>().text =
                string.Format("{0} Status", Ship.GetCompositeSystem().name);

            _systemImageBackgrounds = new Image[Ship.Systems.Length];

            for (var i = 0; i < Ship.Systems.Length; i++)
            {
                var system = Ship.Systems[i];
                var systemBox = Instantiate(SystemTransform, _systemDisplay);
                systemBox.anchorMin = new Vector2(0.2f * system.X, 0.25f * system.Y);
                systemBox.anchorMax = new Vector2(0.2f * (system.X + system.Width), 0.25f * (system.Y + system.Height));
                systemBox.offsetMin = new Vector2(-1, -1);
                systemBox.offsetMax = new Vector2(1, 1);
                _systemImageBackgrounds[i] = systemBox.GetComponent<Image>();
                systemBox.Find("System Name").GetComponent<TextMeshProUGUI>().text = system.System.name;
            }

            var n = 0;
            _compositeSystems = Ship.GetCompositeSystems();

            _compositeOptionAs = new Button[_compositeSystems.Length];
            _compositeOptionBs = new Button[_compositeSystems.Length];

            foreach (var systemIndex in _compositeSystems)
            {
                var index = systemIndex;
                var system = Ship.Systems[systemIndex].System;
                var c = n;
                n++;
                var compositeBox = Instantiate(CompositeTransform, _compositeDisplay);
                compositeBox.Find("System").GetComponent<TextMeshProUGUI>().text = string.Format("System {0}", n);
                var optionA = compositeBox.Find("Option A");
                optionA.GetComponent<Image>().sprite = system.SubSystems[0].Icon;
                var optionAButton = optionA.GetComponent<Button>();
                _compositeOptionAs[c] = optionAButton;
                optionAButton.onClick.AddListener(() => SetCompositeSystem(index, 0));

                var optionB = compositeBox.Find("Option B");
                optionB.GetComponent<Image>().sprite = system.SubSystems[1].Icon;
                var optionBButton = optionB.GetComponent<Button>();
                optionBButton.onClick.AddListener(() => SetCompositeSystem(index, 1));
                _compositeOptionBs[c] = optionBButton;

                compositeBox.transform.localPosition = new Vector3(0, c * -50);
            }

            UpdateSpeed();
            UpdateThrust();
            UpdateInitiative();
            UpdateOrders();
            UpdateSubsystems();
            UpdateDamage();
        }

        private void UpdateDamage()
        {
            _knownDamage = (bool[]) Ship.Damage.Clone();

            for (var i = 0; i < Ship.Damage.Length; i++)
            {
                _systemImageBackgrounds[i].sprite = !Ship.Damage[i] ? SystemBackground : DamagedSystemBackground;
                _systemImageBackgrounds[i].color = !Ship.Damage[i] ? Color.black : Color.red;
            }
        }

        private void UpdateSubsystems()
        {
            _knownSubsystems = (int[]) Ship.Subsystem.Clone();
            _knownOrderable = Ship.Orderable;

            var seen = 0;
            for (var i = 0; i < Ship.Systems.Length; i++)
            {
                if (Ship.Systems[i].System.Type != SystemType.Composite) continue;
                seen++;

                if (Ship.Damage[i])
                {
                    SetInactiveSystem(_compositeOptionAs[seen - 1]);
                    SetInactiveSystem(_compositeOptionBs[seen - 1]);
                    continue;
                }

                if (Ship.Subsystem[i] == -1)
                {
                    if (!Ship.Orderable)
                    {
                        SetInactiveSystem(_compositeOptionAs[seen - 1]);
                        SetInactiveSystem(_compositeOptionBs[seen - 1]);
                        continue;
                    }

                    SetSelectableSystem(_compositeOptionAs[seen - 1]);
                    SetSelectableSystem(_compositeOptionBs[seen - 1]);
                    continue;
                }

                if (!Ship.Orderable)
                {
                    SetSelectedSystem(Ship.Subsystem[i] == 0
                        ? _compositeOptionAs[seen - 1]
                        : _compositeOptionBs[seen - 1]);
                }
                else
                {
                    SetActiveSystem(
                        Ship.Subsystem[i] == 0 ? _compositeOptionAs[seen - 1] : _compositeOptionBs[seen - 1]);
                }

                SetInactiveSystem(Ship.Subsystem[i] == 0 ? _compositeOptionBs[seen - 1] : _compositeOptionAs[seen - 1]);
            }
        }

        private void SetCompositeSystem(int systemNumber, int subsystem)
        {
            Ship.Subsystem[systemNumber] = Ship.Subsystem[systemNumber] == -1 ? subsystem : -1;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        private void Update()
        {
            if (_knownInitiative != Ship.Initiative)
            {
                UpdateInitiative();
            }

            if (_knownSpeed != Ship.Speed)
            {
                UpdateSpeed();
            }

            if (_knownThrust != Ship.ThrustRemaining)
            {
                UpdateThrust();
            }

            if (_knownOrderable != Ship.Orderable)
            {
                UpdateOrders();
                UpdateSubsystems();
            }
            else
            {
                if (_knownOrder != Ship.Order || _knownUnderOrder != Ship.UnderOrders)
                {
                    UpdateOrders();
                }

                if (!_knownSubsystems.SequenceEqual(Ship.Subsystem))
                {
                    UpdateSubsystems();
                }
            }

            if (!_knownDamage.SequenceEqual(Ship.Damage))
            {
                UpdateDamage();
            }
        }

        private void UpdateOrders()
        {
            _knownOrder = Ship.Order;
            _knownUnderOrder = Ship.UnderOrders;
            _knownOrderable = Ship.Orderable;

            if (Ship.Orderable)
            {
                if (Ship.UnderOrders)
                {
                    if (Ship.Order == Order.LockOnTarget) SetActiveSystem(_lockOnTarget);
                    else SetInactiveSystem(_lockOnTarget);

                    if (Ship.Order == Order.BraceForImpact) SetActiveSystem(_braceForImpact);
                    else SetInactiveSystem(_braceForImpact);

                    if (Ship.Order == Order.MilitaryThrust) SetActiveSystem(_fullThrust);
                    else SetInactiveSystem(_fullThrust);

                    if (Ship.Order == Order.OnMyMark) SetActiveSystem(_onMyMark);
                    else SetInactiveSystem(_onMyMark);
                }
                else
                {
                    SetSelectableSystem(_lockOnTarget);
                    SetSelectableSystem(_braceForImpact);
                    SetSelectableSystem(_fullThrust);
                    SetSelectableSystem(_onMyMark);
                }
            }
            else
            {
                if (Ship.UnderOrders)
                {
                    if (Ship.Order == Order.LockOnTarget) SetSelectedSystem(_lockOnTarget);
                    else SetInactiveSystem(_lockOnTarget);

                    if (Ship.Order == Order.BraceForImpact) SetSelectedSystem(_braceForImpact);
                    else SetInactiveSystem(_braceForImpact);

                    if (Ship.Order == Order.MilitaryThrust) SetSelectedSystem(_fullThrust);
                    else SetInactiveSystem(_fullThrust);

                    if (Ship.Order == Order.OnMyMark) SetSelectedSystem(_onMyMark);
                    else SetInactiveSystem(_onMyMark);
                }
                else
                {
                    SetInactiveSystem(_lockOnTarget);
                    SetInactiveSystem(_braceForImpact);
                    SetInactiveSystem(_fullThrust);
                    SetInactiveSystem(_onMyMark);
                }
            }
        }

        private void UpdateThrust()
        {
            _knownThrust = Ship.ThrustRemaining;
            _thrust.text = string.Format("Thrust\n{0}", Ship.ThrustRemaining);
        }

        private void UpdateSpeed()
        {
            _knownSpeed = Ship.Speed;
            _speed.text = string.Format("Speed\n{0}", Ship.Initiative);
        }

        private void UpdateInitiative()
        {
            _knownInitiative = Ship.Initiative;
            _initiative.text = string.Format("Initiative\n{0}", Ship.Initiative);
        }

        private void SetOrder(Order order)
        {
            if (!Ship.Orderable) return;
            Ship.UnderOrders = !Ship.UnderOrders;
            Ship.Order = order;
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        private void SetSelectableSystem(Button button)
        {
            button.interactable = true;
            var colorBlock = button.colors;
            colorBlock.highlightedColor = _factionColor;
            colorBlock.normalColor = Color.black;
            colorBlock.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
            button.colors = colorBlock;
        }

        private void SetInactiveSystem(Button button)
        {
            button.interactable = false;
            var colorBlock = button.colors;
            colorBlock.highlightedColor = _factionColor;
            colorBlock.normalColor = Color.black;
            colorBlock.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
            button.colors = colorBlock;
        }

        private void SetActiveSystem(Button button)
        {
            button.interactable = true;
            var colorBlock = button.colors;
            colorBlock.highlightedColor = _factionColor;
            colorBlock.normalColor = _factionColor;
            colorBlock.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.5f);
            button.colors = colorBlock;
        }

        private void SetSelectedSystem(Button button)
        {
            button.interactable = false;
            var colorBlock = button.colors;
            colorBlock.highlightedColor = _factionColor;
            colorBlock.normalColor = _factionColor;
            colorBlock.disabledColor = _factionColor;
            button.colors = colorBlock;
        }
    }
}