using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class OgrePlanUI : MonoBehaviour
{
    public TextMeshProUGUI ogreNameText;
    public TextMeshProUGUI currentTaskText;
    public TextMeshProUGUI planListText;

    public void SetOgreName(string name)
    {
        ogreNameText.text = name;
    }

    public void UpdateCurrentTask(string task)
    {
        currentTaskText.text = "Current: " + task;
    }

    public void UpdatePlanList(IEnumerable<string> tasks)
    {
        planListText.text = "Plan: \n";
        foreach (var t in tasks)
            planListText.text += "- " + t + "\n";
    }
}