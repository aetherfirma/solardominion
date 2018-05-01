#if UNITY_EDITOR
using UnityEditor;

namespace Logic.Gameplay.Ships
{
    [CustomEditor(typeof(ShipSystem))]
    public class ShipSystemEditor : Editor
    {
        public SerializedProperty Type;
        public SerializedProperty SubSystems;
        public SerializedProperty Thrust;
        public SerializedProperty Defence;
        public SerializedProperty Shots, Range, Damage;
        public SerializedProperty Orders;
        public SerializedProperty Model;
        public SerializedProperty Displayed;
        public SerializedProperty Cost;
        public SerializedProperty CardImages;
        public SerializedProperty Icon;

        private void OnEnable()
        {
            Type = serializedObject.FindProperty("Type");
            SubSystems = serializedObject.FindProperty("SubSystems");
            Thrust = serializedObject.FindProperty("Thrust");
            Defence = serializedObject.FindProperty("Defence");
            Shots = serializedObject.FindProperty("Shots");
            Range = serializedObject.FindProperty("Range");
            Damage = serializedObject.FindProperty("Damage");
            Orders = serializedObject.FindProperty("Orders");
            Model = serializedObject.FindProperty("Model");
            Displayed = serializedObject.FindProperty("Displayed");
            Cost = serializedObject.FindProperty("Cost");
            Cost = serializedObject.FindProperty("Cost");
            CardImages = serializedObject.FindProperty("CardImages");
            Icon = serializedObject.FindProperty("Icon");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(Type);
            switch (Type.enumValueIndex)
            {
                case 0:
                    EditorGUILayout.PropertyField(Thrust);
                    break;
                case 1:
                    EditorGUILayout.PropertyField(Shots);
                    EditorGUILayout.PropertyField(Range);
                    EditorGUILayout.PropertyField(Damage);
                    break;
                case 2:
                    EditorGUILayout.PropertyField(Orders, true);
                    break;
                case 3:
                    break;
                case 4:
                    EditorGUILayout.PropertyField(Defence);
                    break;
                case 5:
                    EditorGUILayout.PropertyField(SubSystems, true);
                    break;
            }
            EditorGUILayout.PropertyField(CardImages, true);
            EditorGUILayout.PropertyField(Icon);
            EditorGUILayout.PropertyField(Cost, true);
            EditorGUILayout.PropertyField(Displayed);
            if (Displayed.boolValue) EditorGUILayout.PropertyField(Model);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
