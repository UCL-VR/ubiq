using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Ubiq.Logging;
using UnityEngine;

[RequireComponent(typeof(QuestionnaireController))]
public class BrowserQuestionnaireExperimentManager : MonoBehaviour
{
    private QuestionnaireController questionnaire;

    private void Awake()
    {
        questionnaire = GetComponent<QuestionnaireController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        cylinders.Add(Cylinder1);
        cylinders.Add(Cylinder2);
        cylinders.Add(Cylinder3);

        conditions.Add(new Condition()
        {
            Weight1 = 1f,
            Weight2 = 0.5f,
            Weight3 = 0.2f
        });

        conditions.Add(new Condition()
        {
            Weight1 = 0.5f,
            Weight2 = 1.5f,
            Weight3 = 0.2f
        });

        conditions.Add(new Condition()
        {
            Weight1 = 0.15f,
            Weight2 = 0.5f,
            Weight3 = 0.2f
        });

        log = new ExperimentLogEmitter(this);

        StartCoroutine(ExperimentRunner());
    }

    public BrowserQuestionnaireExperimentCylinder Cylinder1;
    public BrowserQuestionnaireExperimentCylinder Cylinder2;
    public BrowserQuestionnaireExperimentCylinder Cylinder3;
    public TextMeshProUGUI Instructions;

    private ExperimentLogEmitter log;

    private List<BrowserQuestionnaireExperimentCylinder> cylinders = new List<BrowserQuestionnaireExperimentCylinder>();

    private class Condition
    {
        public int Id;
        public float Weight1;
        public float Weight2;
        public float Weight3;
    }

    private List<Condition> conditions = new List<Condition>();

    IEnumerator ExperimentRunner()
    {
        foreach (var condition in conditions)
        {
            SetUpCondition(condition);

            Instructions.text = "Please pick up each Cylinder at least once.";

            yield return WaitForCylinders();

            Instructions.text = "Please take off the headset and complete the questionnaire";

            yield return WaitForQuestionnaire();
        }

        Instructions.text = "Experiment complete - thank you for taking part - you can now take off the headset";
    }

    IEnumerator WaitForCylinders()
    {
        foreach (var item in cylinders)
        {
            item.HasBeenGrasped = false;
        }

        while(cylinders.Any(item => !item.HasBeenGrasped))
        {
            yield return 0;
        }
    }

    IEnumerator WaitForQuestionnaire()
    {
        yield return questionnaire.DoQuestionnaire();
    }

    void SetUpCondition(Condition condition)
    {
        Cylinder1.SetWeight(condition.Weight1);
        Cylinder2.SetWeight(condition.Weight2);
        Cylinder3.SetWeight(condition.Weight3);
        log.Log("Condition",condition);
    }


}
