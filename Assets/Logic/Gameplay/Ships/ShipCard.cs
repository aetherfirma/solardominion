using System;
using System.Linq;
using Logic.Gameplay.Players;
using Logic.Ui;
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
        private Button _lockOnTarget, _braceForImpact, _fullThrust;
        private Button[] _compositeOptionAs, _compositeOptionBs;
        private TextMeshProUGUI _initiative, _speed, _thrust;
        public Sprite SystemBackground, DamagedSystemBackground, SelectableSystemBackground;
        public MessageTooltip Tooltip;

        public delegate bool SystemCallback(ShipCard card, int i, ShipSystem system);

        public SystemCallback Callback;

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
        private Button[] _systemButtons;
        private bool[] _knownDamage, _knownSelected, _knownSelectable;

        public bool[] Selectable;
        public bool[] Selected;

        private void Start()
        {
            _factionColor = Ship.Faction == Faction.UNM ? _unmColor : _ip3Color;
            
            Selectable = new bool[Ship.Systems.Length];
            Selected = new bool[Ship.Systems.Length];

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
            var tooltip = _lockOnTarget.gameObject.AddComponent<TooltipHaver>();
            tooltip.Tooltip = Tooltip;
            tooltip.Message = "<b><size=16>Lock on Target</size><b>\nRerolls failed attack rolls against the first ship you shoot, rerolls successful rolls against any other ship.";

            _braceForImpact = transform.Find("Order Icons/Brace For Impact").GetComponent<Button>();
            colorBlock = _braceForImpact.colors;
            colorBlock.highlightedColor = _factionColor;
            _braceForImpact.colors = colorBlock;
            _braceForImpact.onClick.AddListener(() => SetOrder(Order.BraceForImpact));
            tooltip = _braceForImpact.gameObject.AddComponent<TooltipHaver>();
            tooltip.Tooltip = Tooltip;
            tooltip.Message = "<b><size=16>Brace for Impact</size><b>\nRerolls failed defence rolls, rerolls successful attack rolls.";

            _fullThrust = transform.Find("Order Icons/Full Thrust").GetComponent<Button>();
            colorBlock = _fullThrust.colors;
            colorBlock.highlightedColor = _factionColor;
            _fullThrust.colors = colorBlock;
            _fullThrust.onClick.AddListener(() => SetOrder(Order.MilitaryThrust));
            tooltip = _fullThrust.gameObject.AddComponent<TooltipHaver>();
            tooltip.Tooltip = Tooltip;
            tooltip.Message = "<b><size=16>Full Thrust</size><b>\nDoubles the thrust provided by all systems, but rerolls successful defence rolls.";

            transform.Find("Rating").GetComponent<TextMeshProUGUI>().text = string.Format("Rating\n{0}", Ship.Training);
            _initiative = transform.Find("Initiative").GetComponent<TextMeshProUGUI>();
            _speed = transform.Find("Speed").GetComponent<TextMeshProUGUI>();
            _thrust = transform.Find("Thrust").GetComponent<TextMeshProUGUI>();

            transform.Find("Composite System Title").GetComponent<TextMeshProUGUI>().text =
                string.Format("{0} Status", Ship.GetCompositeSystem().name);

            _systemButtons = new Button[Ship.Systems.Length];

            for (var i = 0; i < Ship.Systems.Length; i++)
            {
                var system = Ship.Systems[i];
                var systemBox = Instantiate(SystemTransform, _systemDisplay);
                systemBox.anchorMin = new Vector2(0.2f * system.X, 0.25f * system.Y);
                systemBox.anchorMax = new Vector2(0.2f * (system.X + system.Width), 0.25f * (system.Y + system.Height));
                systemBox.offsetMin = new Vector2(-1, -1);
                systemBox.offsetMax = new Vector2(1, 1);
                _systemButtons[i] = systemBox.GetComponent<Button>();

                tooltip = systemBox.gameObject.AddComponent<TooltipHaver>();
                tooltip.Tooltip = Tooltip;
                tooltip.Message = system.System.Describe();

                _systemButtons[i].interactable = false;
                var index = i;
                _systemButtons[i].onClick.AddListener(() =>
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    if (Callback(this, index, system.System))
                    {
                        Selected[index] = !Selected[index];
                    }
                });

                var buttonColors = _systemButtons[i].colors;
                buttonColors.highlightedColor = _factionColor;
                _systemButtons[i].colors = buttonColors;

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
                compositeBox.transform.localPosition = new Vector3(0, c * -50);

                compositeBox.Find("System").GetComponent<TextMeshProUGUI>().text = string.Format("System {0}", n);
                var optionAButton = compositeBox.Find("Option A").GetComponent<Button>();
                optionAButton.image.sprite = system.SubSystems[0].Icon;
                _compositeOptionAs[c] = optionAButton;
                optionAButton.onClick.AddListener(() => SetCompositeSystem(index, 0));

                tooltip = optionAButton.gameObject.AddComponent<TooltipHaver>();
                tooltip.Tooltip = Tooltip;
                tooltip.Message = system.SubSystems[0].Describe();

                var optionB = compositeBox.Find("Option B");
                optionB.GetComponent<Image>().sprite = system.SubSystems[1].Icon;
                var optionBButton = optionB.GetComponent<Button>();
                optionBButton.onClick.AddListener(() => SetCompositeSystem(index, 1));
                _compositeOptionBs[c] = optionBButton;

                tooltip = optionBButton.gameObject.AddComponent<TooltipHaver>();
                tooltip.Tooltip = Tooltip;
                tooltip.Message = system.SubSystems[1].Describe();

            }

            UpdateSpeed();
            UpdateThrust();
            UpdateInitiative();
            UpdateOrders();
            UpdateSubsystems();
            UpdateSystemsDisplay();
        }

        private void UpdateSystemsDisplay()
        {
            _knownDamage = (bool[]) Ship.Damage.Clone();
            _knownSelectable = (bool[]) Selectable.Clone();
            _knownSelected = (bool[]) Selected.Clone();

            for (var i = 0; i < Ship.Damage.Length; i++)
            {
                var button = _systemButtons[i];
                var image = button.image;
                var damaged = Ship.Damage[i];
                var selectable = Selectable[i];
                var selected = Selected[i];
                var colours = button.colors;

                if (damaged)
                {
                    image.sprite = DamagedSystemBackground;
                    colours.disabledColor = Color.red;
                    button.interactable = false;
                }
                else if (selected)
                {
                    image.sprite = SelectableSystemBackground;
                    colours.normalColor = _factionColor;
                    colours.highlightedColor = _factionColor;
                    colours.pressedColor = Color.grey;
                    button.interactable = true;                    
                }
                else if (selectable)
                {
                    image.sprite = SelectableSystemBackground;
                    colours.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    colours.highlightedColor = _factionColor;
                    colours.pressedColor = _factionColor;
                    button.interactable = true;
                }
                else
                {
                    image.sprite = SystemBackground;
                    colours.disabledColor = Color.black;
                    button.interactable = false;
                }

                button.colors = colours;
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

            if (!_knownDamage.SequenceEqual(Ship.Damage) || !_knownSelected.SequenceEqual(Selected) || !_knownSelectable.SequenceEqual(Selectable))
            {
                UpdateSystemsDisplay();
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
                }
                else
                {
                    SetSelectableSystem(_lockOnTarget);
                    SetSelectableSystem(_braceForImpact);
                    SetSelectableSystem(_fullThrust);
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
                }
                else
                {
                    SetInactiveSystem(_lockOnTarget);
                    SetInactiveSystem(_braceForImpact);
                    SetInactiveSystem(_fullThrust);
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