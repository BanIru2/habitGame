using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;    // 멀티파트 폼클래스 사용 위한 네임스페이스

public class HabitService
{
    private readonly ApiClient apiClient;

    public HabitService(ApiClient apiClient)
    {
        this.apiClient = apiClient;
    }

    // 생활 습관 목표 생성
    public Task<HabitGoalResponse> CreateGoalAsync(CreateHabitGoalRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<CreateHabitGoalRequest, HabitGoalResponse>(
            "/habit-goals",
            request
        );
    }
    // ⭐ 생활 습관 목표 조회
    public Task<List<HabitGoalResponse>> GetGoalsAsync()
    {
        long userId = GetCurrentUserId();

        return apiClient.GetAsync<List<HabitGoalResponse>>(
            $"/habit-goals?userId={userId}"
        );
    }
    // 생활 습관 기록 제출 (인증)
    public Task<HabitRecordResponse> CreateRecordAsync(CreateHabitRecordRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<CreateHabitRecordRequest, HabitRecordResponse>(
            "/habit-records",
            request
        );
    }

    // 생활 습관 보상 수령
    public Task<HabitRewardClaimResponse> ClaimRewardAsync(ClaimHabitRewardRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        request.UserId = GetCurrentUserId();

        return apiClient.PostAsync<ClaimHabitRewardRequest, HabitRewardClaimResponse>(
            "/rewards/claim",
            request
        );
    }

    // 생활 습관 사진 인증 요청
    public Task<HabitVerifyResponse> VerifyHabitPhotoAsync(long goalId, byte[] photoBytes)
    {
        if (goalId <= 0) throw new ArgumentException("유효하지 않은 goalId입니다.", nameof(goalId));

        if (photoBytes == null || photoBytes.Length == 0) throw new ArgumentException("사진 데이터가 비어있습니다.", nameof(photoBytes));

        // 전달용 MultipartFormData 생성
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            // 멀티파트폼 데이터 섹션 : 목표Id
            // 필드명, 목표id 저장
            new MultipartFormDataSection("goalId", goalId.ToString()),
            // 멀티파트폼 파일 섹션 : 사진데이터
            // 필드명, 사진데이터(바이트배열), 파일명, 확장자 타입(MIME) 저장
            new MultipartFormFileSection("photo", photoBytes, "habit_photo.jpg", "image/jpeg")
        };

        return apiClient.PostMultipartAsync<HabitVerifyResponse>("/api/habits/verify", formData);
    }
    private long GetCurrentUserId()
    {
        long userId = apiClient.CurrentUserId;
        if (userId <= 0)
            throw new InvalidOperationException("로그인이 필요합니다.");

        return userId;
    }
}
