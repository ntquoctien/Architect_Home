using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom Editor cho WallEditor - hiển thị UI thêm/xóa tường trong Inspector
/// </summary>
[CustomEditor(typeof(WallEditor))]
public class WallEditorInspector : Editor
{
    private WallEditor wallEditor;
    private WallEditor.WallPosition selectedPosition = WallEditor.WallPosition.NorthEast;
    private float customWallLength = 0f;
    private string wallToRemove = "";

    // Interior wall
    private Vector3 interiorStart = Vector3.zero;
    private Vector3 interiorEnd = new Vector3(4f, 0f, 0f);

    private void OnEnable()
    {
        wallEditor = (WallEditor)target;
    }

    public override void OnInspectorGUI()
    {
        // Vẽ Inspector mặc định
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Wall Management", EditorStyles.boldLabel);

        // Hiển thị thông tin tường hiện có
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            int wallCount = wallEditor.GetWallCount();
            EditorGUILayout.LabelField($"Số tường hiện có: {wallCount}");
            EditorGUILayout.LabelField($"Có thể thêm: {wallEditor.CanAddWall()}");
            EditorGUILayout.LabelField($"Có thể xóa: {wallEditor.CanRemoveWall()}");

            if (GUILayout.Button("Refresh Wall List"))
            {
                wallEditor.RefreshWallDict();
                Debug.Log($"Danh sách tường: {string.Join(", ", wallEditor.GetAllWallNames())}");
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // === THÊM TƯỜNG ===
        EditorGUILayout.LabelField("Thêm Tường", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            selectedPosition = (WallEditor.WallPosition)EditorGUILayout.EnumPopup("Vị trí", selectedPosition);
            customWallLength = EditorGUILayout.FloatField("Chiều dài (0 = auto)", customWallLength);

            GUI.enabled = wallEditor.CanAddWall();
            if (GUILayout.Button("+ Thêm Tường"))
            {
                Undo.RecordObject(wallEditor, "Add Wall");
                wallEditor.AddWall(selectedPosition, customWallLength);
                EditorUtility.SetDirty(wallEditor);
            }
            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // === THÊM TƯỜNG NỘI THẤT ===
        EditorGUILayout.LabelField("Thêm Tường Nội Thất (Chia Phòng)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            interiorStart = EditorGUILayout.Vector3Field("Điểm bắt đầu (local)", interiorStart);
            interiorEnd = EditorGUILayout.Vector3Field("Điểm kết thúc (local)", interiorEnd);

            GUI.enabled = wallEditor.CanAddWall();
            if (GUILayout.Button("+ Thêm Tường Nội Thất"))
            {
                Undo.RecordObject(wallEditor, "Add Interior Wall");
                wallEditor.AddInteriorWall(interiorStart, interiorEnd);
                EditorUtility.SetDirty(wallEditor);
            }
            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // === XÓA TƯỜNG ===
        EditorGUILayout.LabelField("Xóa Tường", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        {
            var wallNames = wallEditor.GetAllWallNames();
            if (wallNames.Count > 0)
            {
                // Dropdown chọn tường
                int selectedIndex = Mathf.Max(0, wallNames.IndexOf(wallToRemove));
                selectedIndex = EditorGUILayout.Popup("Chọn tường", selectedIndex, wallNames.ToArray());
                if (selectedIndex >= 0 && selectedIndex < wallNames.Count)
                    wallToRemove = wallNames[selectedIndex];

                GUI.enabled = wallEditor.CanRemoveWall() && !string.IsNullOrEmpty(wallToRemove);
                
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("- Xóa Tường"))
                    {
                        if (EditorUtility.DisplayDialog("Xác nhận", $"Bạn có chắc muốn xóa {wallToRemove}?", "Xóa", "Hủy"))
                        {
                            Undo.RecordObject(wallEditor, "Remove Wall");
                            wallEditor.RemoveWall(wallToRemove);
                            EditorUtility.SetDirty(wallEditor);
                            wallToRemove = "";
                        }
                    }

                    if (GUILayout.Button("Ẩn/Hiện"))
                    {
                        var walls = wallEditor.GetAllWalls();
                        var wall = walls.Find(w => w.name == wallToRemove);
                        if (wall != null)
                        {
                            Undo.RecordObject(wall.gameObject, "Toggle Wall Visibility");
                            wallEditor.SetWallVisible(wallToRemove, !wall.gameObject.activeSelf);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                GUI.enabled = true;
            }
            else
            {
                EditorGUILayout.HelpBox("Không có tường nào.", MessageType.Info);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // === THÊM NHANH CÁC TƯỜNG GÓC ===
        EditorGUILayout.LabelField("Thêm Nhanh", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        {
            GUI.enabled = wallEditor.CanAddWall();

            if (GUILayout.Button("+ NE"))
                wallEditor.AddWall(WallEditor.WallPosition.NorthEast);

            if (GUILayout.Button("+ NW"))
                wallEditor.AddWall(WallEditor.WallPosition.NorthWest);

            if (GUILayout.Button("+ SE"))
                wallEditor.AddWall(WallEditor.WallPosition.SouthEast);

            if (GUILayout.Button("+ SW"))
                wallEditor.AddWall(WallEditor.WallPosition.SouthWest);

            GUI.enabled = true;
        }
        EditorGUILayout.EndHorizontal();
    }
}
