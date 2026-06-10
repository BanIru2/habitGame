using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HabitTest : MonoBehaviour
{
    private HabitService habitService;
    private CreateHabitGoalRequest request;
    private HabitGoalResponse response;

    [SerializeField]
    private Button button;
    // ----------------------임시 요청용 데이터셋-------------------------
    [SerializeField]
    private HabitCategory targetCategory;
    [SerializeField]
    private float targetAmount;
    [SerializeField]
    private string unit;
    [SerializeField]
    private string period;
    // -------------------------------------------------------------------


    private void Start()
    {
        habitService = ServiceRegistry.Instance.Habit;
    }

    private void Awake()
    {
        button.onClick.AddListener(OnClickCreateGoal);
    }

    private async void OnClickCreateGoal()
    {
        LifeStyleHabitCategorySO categorySO = SORegistry.Instance.GetLifeStyleHabitCategory(targetCategory);
        try
        {
            button.interactable = false;
            request = new CreateHabitGoalRequest { Category = HabitCategoryMapper.ToCategoryId(targetCategory), 
                TargetAmount = targetAmount, Unit = unit, Period = period };
            response = await habitService.CreateGoalAsync(request);
            
            if(response != null)
            {
                Debug.Log($"목표 생성 성공 : {response.Category} {response.Unit}");
                Debug.Log($"예상 상승 스탯 : {categorySO.PrimaryStat}, {categorySO.SecondaryStat}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[HabitTest] 목표 생성 실패: {e.Message}");
        }
        finally
        {
            button.interactable = true;
        }
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnClickCreateGoal);
    }
}
