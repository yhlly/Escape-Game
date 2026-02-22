using UnityEngine;

[CreateAssetMenu(menuName = "EscapeRoom/Document Data", fileName = "Doc_")]
public class DocumentData : ScriptableObject
{
    public string id;                // 唯一ID：例如 "office_consent_fake"
    public string title;
    [TextArea(8, 30)]
    public string body;
    public Sprite image;             // 可选：证据图片/扫描件
}
