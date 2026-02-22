using System;
using UnityEngine;

/// <summary>
/// 优化版：现在继承自ActShowDocument，只需要提供手环字段，复用文档显示逻辑
/// </summary>
public class ActShowWristband : ActShowDocument
{
    [Header("Wristband Fields")]
    public string patientName = "Lu";
    public string sex = "F";
    public int age = 19;

    [Tooltip("Ward number like 302")]
    public int wardNumber = 302;

    [Tooltip("Admit date will be Today - admitDaysAgo")]
    public int admitDaysAgo = 7;

    public override void Execute(GameManager gm)
    {
        if (gm == null) return;

        // 如果没有预设的document，动态创建一个
        if (document == null)
        {
            var doc = ScriptableObject.CreateInstance<DocumentData>();
            doc.id = "wristband_" + wardNumber;
            doc.title = "Patient Wristband";
            doc.body = BuildWristbandBody();
            document = doc;
        }

        // 复用父类的显示逻辑
        base.Execute(gm);
    }

    private string BuildWristbandBody()
    {
        DateTime admitDate = DateTime.Now.Date.AddDays(-Mathf.Abs(admitDaysAgo));

        return
            $"Name: {patientName}\n" +
            $"Sex: {sex}\n" +
            $"Age: {age}\n" +
            $"Admit Date: {admitDate:dd MMM yyyy}\n" +
            $"Ward: {wardNumber}";
    }
}