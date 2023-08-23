using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(BoardConfig))]
public class BoardConfigDrawer : PropertyDrawer
{
    private int _toolbarSelected;
    private readonly string[] _toolbarStrings = { "Eraser", "Square", "Card", "Terrain", "Connection" };

    private int _squareSelectedIndex;
    private bool _connecting;

    private bool IsSameSquare(SerializedProperty square, int otherRow, int otherColumn)
    {
        var coordinate = square.FindPropertyRelative("coordinate");
        int row = coordinate.FindPropertyRelative("row").intValue;
        int column = coordinate.FindPropertyRelative("column").intValue;
        return row == otherRow && column == otherColumn;
    }

    private void SetSquareCoordinate(SerializedProperty square, int row, int column)
    {
        var coordinate = square.FindPropertyRelative("coordinate");
        coordinate.FindPropertyRelative("row").intValue = row;
        coordinate.FindPropertyRelative("column").intValue = column;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (Application.isPlaying)
        {
            return;
        }
        var resolveConnectionTimeProperty = property.FindPropertyRelative("resolveConnectionTime");
        var widthProperty = property.FindPropertyRelative("width");
        var heightProperty = property.FindPropertyRelative("height");
        var squaresProperty = property.FindPropertyRelative("squareConfigs");
        var cardSelector = property.FindPropertyRelative("cardSelector");
        var terrainSelector = property.FindPropertyRelative("terrainSelector");
        var connectionsProperty = property.FindPropertyRelative("connectionConfigs");
        var connectionPrefab = property.FindPropertyRelative("connectionPrefab");
        var connectionPositiveColor = property.FindPropertyRelative("positiveColor");
        var connectionNeutralColor = property.FindPropertyRelative("neutralColor");
        var squareOffset = property.FindPropertyRelative("offset");
        var squarePrefab = property.FindPropertyRelative("squarePrefab");

        EditorGUI.BeginProperty(position, label, property);

        EditorGUILayout.LabelField("Board Config");

        EditorGUILayout.BeginVertical();
        
        var resolveConnectionTime = EditorGUILayout.FloatField("Resolve Connection Time" ,resolveConnectionTimeProperty.floatValue);
        
        var width = EditorGUILayout.IntField("Width", widthProperty.intValue);
        var height = EditorGUILayout.IntField("Height", heightProperty.intValue);

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical();

        for (int i = height-1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int j = 0; j <width; j++)
            {
                if (GUILayout.Button($"{j},{i}"))
                {
                    bool hasSquare = false;
                    int currentIndex = 0;

                    for (int k = 0; k < squaresProperty.arraySize; k++)
                    {
                        var square = squaresProperty.GetArrayElementAtIndex(k);
                        if (IsSameSquare(square, j, i))
                        {
                            hasSquare = true;
                            currentIndex = k;
                        }
                    }
                    
                    bool hasConnection = false;
                    int connectionIndex = 0;
                    
                    for (int k = 0; k < connectionsProperty.arraySize; k++)
                    {
                        var connection = connectionsProperty.GetArrayElementAtIndex(k);
                        var fromSquare = connection.FindPropertyRelative("fromSquare");
                        if (IsSameSquare(fromSquare, j, i))
                        {
                            hasConnection = true;
                            connectionIndex = k;
                        }
                        else
                        {
                            var toSquare = connection.FindPropertyRelative("toSquare");
                            if (IsSameSquare(toSquare, j, i))
                            {
                                hasConnection = true;
                                connectionIndex = k;
                            }
                        }
                    }

                    if (_toolbarSelected == 0)
                    {
                        _connecting = false;
                        if (hasConnection)
                        {
                            connectionsProperty.DeleteArrayElementAtIndex(connectionIndex);
                        }   
                        if (hasSquare)
                        {
                            squaresProperty.DeleteArrayElementAtIndex(currentIndex);
                        }
                    }

                    if (_toolbarSelected >= 1 && _toolbarSelected < 4)
                    {
                        _connecting = false;
                        if (!hasSquare)
                        {
                            squaresProperty.InsertArrayElementAtIndex(currentIndex);
                            var squareToCreate = squaresProperty.GetArrayElementAtIndex(currentIndex);
                            SetSquareCoordinate(squareToCreate, j, i);
                        }

                        var traittable = squaresProperty.GetArrayElementAtIndex(currentIndex)
                            .FindPropertyRelative("traittableConfig");
                        if (traittable.objectReferenceValue != null)
                        {
                            traittable.objectReferenceValue = null;
                        }

                        if (_toolbarSelected == 2)
                        {
                            string cardName = cardSelector.FindPropertyRelative(("cardName")).stringValue;
                            var card = Resources.Load("Cards/" + cardName);
                            traittable.objectReferenceValue = card;
                        }

                        if (_toolbarSelected == 3)
                        {
                            string terrainName = terrainSelector.FindPropertyRelative(("terrainName")).stringValue;
                            var terrain = Resources.Load("Terrains/" + terrainName);
                            traittable.objectReferenceValue = terrain;
                        }
                    }

                    if (_toolbarSelected == 4)
                    {
                        if (hasSquare)
                        {
                            if (!_connecting)
                            {
                                Debug.Log("Conectando");
                                _connecting = true;
                                _squareSelectedIndex = currentIndex;
                                //Iluminar cuadrado.
                            }
                            else
                            {
                                _connecting = false;
                                if(currentIndex==_squareSelectedIndex) continue;
                                var fromSquare = squaresProperty.GetArrayElementAtIndex(_squareSelectedIndex);
                                var fromSquareCoordinate = fromSquare.FindPropertyRelative("coordinate");
                                int fromRow = fromSquareCoordinate.FindPropertyRelative("row").intValue;
                                int fromColumn = fromSquareCoordinate.FindPropertyRelative("column").intValue;
                                
                                var toSquare = squaresProperty.GetArrayElementAtIndex(currentIndex);
                                var toSquareCoordinate = toSquare.FindPropertyRelative("coordinate");
                                int toRow = toSquareCoordinate.FindPropertyRelative("row").intValue;
                                int toColumn = toSquareCoordinate.FindPropertyRelative("column").intValue;
                                
                                bool exist = false;
                                for (int k = 0; k < connectionsProperty.arraySize; k++)
                                {
                                    var fromExistingCoordinate = connectionsProperty.GetArrayElementAtIndex(k).FindPropertyRelative("fromSquare").FindPropertyRelative("coordinate");
                                    int fromExistingRow = fromExistingCoordinate.FindPropertyRelative("row").intValue;
                                    int fromExistingColumn = fromExistingCoordinate.FindPropertyRelative("column").intValue;
                                    
                                    var toExistingCoordinate = connectionsProperty.GetArrayElementAtIndex(k).FindPropertyRelative("toSquare").FindPropertyRelative("coordinate");
                                    int toExistingRow = toExistingCoordinate.FindPropertyRelative("row").intValue;
                                    int toExistingColumn = toExistingCoordinate.FindPropertyRelative("column").intValue;

                                    if ((fromExistingRow == fromRow && fromExistingColumn == fromColumn &&
                                         toExistingRow == toRow && toExistingColumn == toColumn) ||
                                        (fromExistingRow == toRow && fromExistingColumn == toColumn &&
                                        toExistingRow == fromRow && toExistingColumn == fromColumn))
                                    {
                                        Debug.Log("Ya existe esa conexión");
                                        exist = true;
                                        break;
                                    }
                                }

                                if (!exist)
                                {
                                    connectionsProperty.InsertArrayElementAtIndex(0);

                                    var fromConnectionSquare = connectionsProperty.GetArrayElementAtIndex(0)
                                        .FindPropertyRelative("fromSquare");
                                    var toConnectionSquare = connectionsProperty.GetArrayElementAtIndex(0)
                                        .FindPropertyRelative("toSquare");
                                
                                    SetSquareCoordinate(fromConnectionSquare, fromRow, fromColumn);
                                    SetSquareCoordinate(toConnectionSquare, j, i);
                                }
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        _toolbarSelected = GUILayout.Toolbar(_toolbarSelected, _toolbarStrings);

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.PropertyField(cardSelector);
        EditorGUILayout.PropertyField(terrainSelector);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Squares");
        for (int i = 0; i < squaresProperty.arraySize; i++)
        {
            var coordinate = squaresProperty.GetArrayElementAtIndex(i).FindPropertyRelative("coordinate");
            int row = coordinate.FindPropertyRelative("row").intValue;
            int column = coordinate.FindPropertyRelative("column").intValue;
            if (row > width - 1 || column > height - 1)
            {
                squaresProperty.DeleteArrayElementAtIndex(i);
            }
            else
            {
                var traittable = squaresProperty.GetArrayElementAtIndex(i).FindPropertyRelative("traittableConfig");
                if (traittable.objectReferenceValue != null)
                {
                    EditorGUILayout.LabelField($"{row}, {column} : {traittable.objectReferenceValue.name}");
                }
                else
                {
                    EditorGUILayout.LabelField($"{row}, {column}");
                }
            }
        }
        
        EditorGUILayout.PropertyField(squarePrefab);
        EditorGUILayout.PropertyField(squareOffset);
        EditorGUILayout.PropertyField(connectionPrefab);
        EditorGUILayout.PropertyField(connectionPositiveColor);
        EditorGUILayout.PropertyField(connectionNeutralColor);

        for (int i = 0; i < connectionsProperty.arraySize; i++)
        {
            
            var fromSquare = connectionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("fromSquare");
            var toSquare = connectionsProperty.GetArrayElementAtIndex(i).FindPropertyRelative("toSquare");
            var fromCoordinate = fromSquare.FindPropertyRelative("coordinate");
            var toCoordinate = toSquare.FindPropertyRelative("coordinate");
            int fromRow = fromCoordinate.FindPropertyRelative("row").intValue;
            int fromColumn = fromCoordinate.FindPropertyRelative("column").intValue;
            int toRow = toCoordinate.FindPropertyRelative("row").intValue;
            int toColumn = toCoordinate.FindPropertyRelative("column").intValue;
            
            if (fromCoordinate==null||fromRow > width - 1 ||fromCoordinate==null || fromColumn > height - 1
                ||toCoordinate==null||toRow > width - 1 ||toCoordinate==null || toColumn > height - 1)
            {
                connectionsProperty.DeleteArrayElementAtIndex(i);
                continue;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{fromRow}, {fromColumn}");
            GUILayout.Label($"{toRow}, {toColumn}");
            if (GUILayout.Button("Delete"))
            {
                connectionsProperty.DeleteArrayElementAtIndex(i);
            }

            GUILayout.EndHorizontal();
        }
        
        EditorGUILayout.HelpBox("Para ver visualmente el nivel, pulsa el botón DrawLevel", MessageType.Info);
        
        EditorGUILayout.EndVertical();

        resolveConnectionTimeProperty.floatValue = resolveConnectionTime;
        widthProperty.intValue = width;
        heightProperty.intValue = height;
        property.serializedObject.ApplyModifiedProperties();

        EditorGUI.EndProperty();
    }
}