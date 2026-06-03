using UnityEngine;

/// <summary>
/// Network Service 인스턴스에 접근하기 위한 통로
/// </summary>
public class ServiceRegistry : Singleton<ServiceRegistry>
{
    [SerializeField] private ApiClient apiClient;

    public AuthService Auth { get; private set; }
    public CharacterService Character { get; private set; }
    public HabitService Habit { get; private set; }
    public SpendingService Spending { get; private set; }
    public InventoryService Inventory { get; private set; }
    public ShopService Shop { get; private set; }
    public BattleService Battle { get; private set; }
    public RankingService Ranking { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        if (apiClient == null)
            apiClient = ApiClient.Instance;

        BuildServices();
    }

    private void BuildServices()
    {
        Auth = new AuthService(apiClient);
        Character = new CharacterService(apiClient);
        Habit = new HabitService(apiClient);
        Spending = new SpendingService(apiClient);
        Inventory = new InventoryService(apiClient);
        Shop = new ShopService(apiClient);
        Battle = new BattleService(apiClient);
        Ranking = new RankingService(apiClient);
    }
}
