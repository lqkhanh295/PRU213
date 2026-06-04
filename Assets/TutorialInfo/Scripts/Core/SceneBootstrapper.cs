using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tự động dựng màn chơi khổng lồ mang phong cách núi rừng Việt Bắc.
/// </summary>
public class SceneBootstrapper : MonoBehaviour
{
    [Header("Auto Build")]
    [SerializeField] public bool buildOnStart = false;

    [Header("3D Models Prefabs")]
    [SerializeField] public GameObject playerModel;
    [SerializeField] public GameObject enemyModel;
    [SerializeField] public GameObject escortModel;
    [SerializeField] public GameObject stiltHouseModel;
    [SerializeField] public GameObject watchtowerModel;
    [SerializeField] public GameObject mountainModel;
    [SerializeField] public GameObject bushModel;
    [SerializeField] public GameObject treeModel;
    [SerializeField] public GameObject bambooModel;
    [SerializeField] public GameObject lanternModel;
    [SerializeField] public GameObject letterModel;
    [SerializeField] public GameObject flagModel;
    [SerializeField] public GameObject fenceModel;

    [Header("Animator Controllers")]
    [SerializeField] public RuntimeAnimatorController playerController;
    [SerializeField] public RuntimeAnimatorController enemyController;
    [SerializeField] public RuntimeAnimatorController escortController;

    private GameObject playerObj;
    private List<GameObject> enemies = new();

    private void Start()
    {
        if (buildOnStart) 
        {
            StartCoroutine(BuildScene());
        }
        else
        {
            // Tự động bật Main Menu nếu không build ngay
            if (GetComponent<MainMenuUI>() == null)
                gameObject.AddComponent<MainMenuUI>();
        }
    }

    public IEnumerator BuildScene()
    {
        BuildTerrain();
        BuildEnvironment();
        BuildPlayer();
        
        yield return null;
        BakeNavMeshRuntime();
        yield return null; 

        BuildEnemies();
        BuildEscort();
        BuildObjectives();
        BuildLighting();
        SetupCamera();
        SetupManagers();

        Debug.Log("[SceneBootstrapper] Scene Việt Bắc đã sẵn sàng!");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TERRAIN & MÔI TRƯỜNG
    // ═══════════════════════════════════════════════════════════════════════════

    private void BuildTerrain()
    {
        // Bản đồ mở rộng 100x100
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name       = "Ground";
        ground.transform.localScale = new Vector3(10f, 1f, 10f); 
        ground.GetComponent<Renderer>().material = MaterialLibrary.Ground();
        ground.tag = "Ground";

        // Tường bao quanh (Tránh rớt map)
        CreateWall(new Vector3(0, 5, 50),  new Vector3(100, 10, 1f), "Wall_North");
        CreateWall(new Vector3(0, 5, -50), new Vector3(100, 10, 1f), "Wall_South");
        CreateWall(new Vector3(50, 5, 0),  new Vector3(1f, 10, 100), "Wall_East");
        CreateWall(new Vector3(-50, 5, 0), new Vector3(1f, 10, 100), "Wall_West");

        // Dãy núi cản đường tạo thành 2 lối đi chính
        BuildMountain(new Vector3(-15, 0, -25), new Vector3(30, 8, 15));
        BuildMountain(new Vector3(10, 0, 20), new Vector3(40, 12, 20));
        BuildMountain(new Vector3(-35, 0, 15), new Vector3(20, 10, 30));
    }

    private void BuildEnvironment()
    {
        // ─── TUYẾN 1: ĐƯỜNG RỪNG & SUỐI (SAFE PATH) ──────────────────────────
        // Suối nước dọc theo rìa Tây và Bắc
        CreateWaterZone(new Vector3(-45, 0.05f, 0), new Vector3(8f, 0.1f, 80f));
        CreateWaterZone(new Vector3(0, 0.05f, 40), new Vector3(80f, 0.1f, 8f));
        
        // Rừng rậm & Bụi rậm bao phủ Tuyến 1
        for(int i=0; i<8; i++) {
            CreateHidingZone(new Vector3(-42 + Random.Range(-3,3), 0, -30 + i*10), new Vector3(6, 0.01f, 6), "Bush_River1_"+i);
            CreateHidingZone(new Vector3(-20 + i*10, 0, 42 + Random.Range(-3,3)), new Vector3(6, 0.01f, 6), "Bush_River2_"+i);
            SpawnBamboo(new Vector3(-35, 0, -30 + i*10));
            SpawnTree(new Vector3(-38, 0, -25 + i*10));
        }
        
        // ─── TUYẾN 2: TRẠI GIẶC (RISKY PATH) ───────────────────────────────
        // Ngôi làng/trạm gác ở giữa bản đồ
        BuildStiltHouse(new Vector3(5, 0, -5));
        BuildStiltHouse(new Vector3(-5, 0, 5));
        BuildStiltHouse(new Vector3(15, 0, -10));

        BuildWatchtower(new Vector3(-12, 0, -12));
        BuildWatchtower(new Vector3(25, 0, 15));

        // Rào chắn trại
        CreateFence(new Vector3(-15, 0, -15), new Vector3(20, 0, -15));
        CreateFence(new Vector3(-15, 0, -15), new Vector3(-15, 0, 10));

        // Đèn lồng thắp sáng khu trại
        SpawnLantern(new Vector3(0, 0, -10));
        SpawnLantern(new Vector3(10, 0, 0));
        SpawnLantern(new Vector3(-10, 0, -5));

        // ─── Props Tương Tác ──────────────────────────────────────────────────
        CreateInteractProp(new Vector3(-25, 0, -35), PlayerInteraction.DisguiseType.ChoppingWood, "Prop_Cui");
        CreateInteractProp(new Vector3(2, 0, -20), PlayerInteraction.DisguiseType.PlayingFlute, "Prop_Sao");
        CreateInteractProp(new Vector3(30, 0, 0), PlayerInteraction.DisguiseType.HerdingBuffalo, "Prop_Trau");

        // Bầy chim (Cảnh báo chạy nhanh)
        CreateBirdFlock(new Vector3(-30, 0.1f, -10));
        CreateBirdFlock(new Vector3(10, 0.1f, 30));
        CreateBirdFlock(new Vector3(-10, 0.1f, 25));
    }

    private void BuildMountain(Vector3 pos, Vector3 size)
    {
        if (mountainModel != null)
        {
            GameObject mt = Instantiate(mountainModel);
            mt.name = "Mountain_3D";
            mt.transform.position = pos;
            mt.transform.localScale = new Vector3(size.x * 0.25f, size.y * 0.25f, size.z * 0.25f);
            AddCollidersToModel(mt);
        }
        else
        {
            GameObject mountain = new GameObject("Mountain");
            mountain.transform.position = pos;
            
            // Tạo núi bằng nhiều khối đá lồng vào nhau
            int chunks = 8;
            for(int i=0; i<chunks; i++)
            {
                GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.transform.SetParent(mountain.transform);
                
                float rx = Random.Range(-size.x/2, size.x/2);
                float rz = Random.Range(-size.z/2, size.z/2);
                float h = Random.Range(size.y * 0.5f, size.y);
                
                rock.transform.localPosition = new Vector3(rx, h/2, rz);
                rock.transform.localScale = new Vector3(Random.Range(size.x*0.4f, size.x*0.8f), h, Random.Range(size.z*0.4f, size.z*0.8f));
                rock.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                
                rock.GetComponent<Renderer>().material = MaterialLibrary.MountainRock();
            }
        }
    }

    private void BuildWatchtower(Vector3 pos)
    {
        if (watchtowerModel != null)
        {
            GameObject tower = Instantiate(watchtowerModel);
            tower.name = "Watchtower_3D";
            tower.transform.position = pos;
            tower.transform.localScale = Vector3.one * 1.5f;
            AddCollidersToModel(tower);
        }
        else
        {
            GameObject tower = new GameObject("Watchtower");
            tower.transform.position = pos;

            // Cột
            for(int x = -1; x <= 1; x+=2) {
                for(int z = -1; z <= 1; z+=2) {
                    GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    p.transform.SetParent(tower.transform);
                    p.transform.localPosition = new Vector3(x * 2.5f, 3f, z * 2.5f);
                    p.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
                    p.GetComponent<Renderer>().material = MaterialLibrary.WoodLog();
                }
            }
            
            // Sàn
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(tower.transform);
            floor.transform.localPosition = new Vector3(0, 6f, 0);
            floor.transform.localScale = new Vector3(6f, 0.2f, 6f);
            floor.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();
            
            // Tường lan can
            GameObject w1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w1.transform.SetParent(tower.transform);
            w1.transform.localPosition = new Vector3(0, 6.5f, 3f);
            w1.transform.localScale = new Vector3(6f, 1f, 0.2f);
            w1.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();

            GameObject w2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w2.transform.SetParent(tower.transform);
            w2.transform.localPosition = new Vector3(0, 6.5f, -3f);
            w2.transform.localScale = new Vector3(6f, 1f, 0.2f);
            w2.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();

            // Mái che (chóp nghiêng)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(tower.transform);
            roof.transform.localPosition = new Vector3(0, 9f, 0);
            roof.transform.localScale = new Vector3(7f, 0.4f, 7f);
            roof.transform.localRotation = Quaternion.Euler(10, 0, 0);
            roof.GetComponent<Renderer>().material = MaterialLibrary.ThatchRoof();

            // Thang
            GameObject ladder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ladder.transform.SetParent(tower.transform);
            ladder.transform.localPosition = new Vector3(0, 3f, -2.5f);
            ladder.transform.localScale = new Vector3(1.5f, 6f, 0.2f);
            ladder.transform.localRotation = Quaternion.Euler(15, 0, 0);
            ladder.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();
        }
    }

    private void BuildStiltHouse(Vector3 pos)
    {
        if (stiltHouseModel != null)
        {
            GameObject house = Instantiate(stiltHouseModel);
            house.name = "StiltHouse_3D";
            house.transform.position = pos;
            house.transform.localScale = Vector3.one * 1.8f;
            AddCollidersToModel(house);
        }
        else
        {
            GameObject house = new GameObject("StiltHouse_Voxel");
            house.transform.position = pos;

            // Cột nhà (Vuông)
            for(int x = -1; x <= 1; x+=2) {
                for(int z = -1; z <= 1; z+=2) {
                    GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    p.transform.SetParent(house.transform);
                    p.transform.localPosition = new Vector3(x * 3f, 1.5f, z * 4f);
                    p.transform.localScale = new Vector3(0.4f, 3f, 0.4f);
                    p.GetComponent<Renderer>().material = MaterialLibrary.WoodLog();
                }
            }

            // Sàn nhà
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(house.transform);
            floor.transform.localPosition = new Vector3(0, 3f, 0);
            floor.transform.localScale = new Vector3(7.5f, 0.3f, 9.5f);
            floor.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();

            // Tường (Tạo 4 bức tường riêng biệt để có Voxel feel)
            CreateVoxelWall(house.transform, new Vector3(3.5f, 4.5f, 0), new Vector3(0.3f, 3f, 8.5f));
            CreateVoxelWall(house.transform, new Vector3(-3.5f, 4.5f, 0), new Vector3(0.3f, 3f, 8.5f));
            CreateVoxelWall(house.transform, new Vector3(0, 4.5f, 4.25f), new Vector3(7.3f, 3f, 0.3f));
            CreateVoxelWall(house.transform, new Vector3(0, 4.5f, -4.25f), new Vector3(7.3f, 3f, 0.3f));

            // Mái ngói Voxel xếp lớp (Dốc xuống 2 bên)
            for (int i = 0; i < 5; i++)
            {
                // Mái phải
                GameObject roofR = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofR.transform.SetParent(house.transform);
                roofR.transform.localPosition = new Vector3(0.5f + i * 0.7f, 7f - i * 0.5f, 0);
                roofR.transform.localScale = new Vector3(0.8f, 0.4f, 10f);
                roofR.transform.localRotation = Quaternion.Euler(0, 0, 30);
                roofR.GetComponent<Renderer>().material = MaterialLibrary.ThatchRoof();

                // Mái trái
                GameObject roofL = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofL.transform.SetParent(house.transform);
                roofL.transform.localPosition = new Vector3(-0.5f - i * 0.7f, 7f - i * 0.5f, 0);
                roofL.transform.localScale = new Vector3(0.8f, 0.4f, 10f);
                roofL.transform.localRotation = Quaternion.Euler(0, 0, -30);
                roofL.GetComponent<Renderer>().material = MaterialLibrary.ThatchRoof();
            }
        }
    }

    private void CreateVoxelWall(Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent);
        wall.transform.localPosition = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = MaterialLibrary.Wall();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // NHÂN VẬT & ĐỊCH
    // ═══════════════════════════════════════════════════════════════════════════

    private void BuildPlayer()
    {
        if (playerModel != null)
        {
            playerObj = Instantiate(playerModel);
            playerObj.name = "KimDong_Player";
        }
        else
        {
            playerObj = new GameObject("KimDong_Player");
        }
        playerObj.tag = "Player";
        playerObj.transform.position = new Vector3(-45, 0.9f, -45);

        if (playerModel == null)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(playerObj.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(0.5f, 0.9f, 0.5f);
            body.GetComponent<Renderer>().material = MaterialLibrary.Player();
            Destroy(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(playerObj.transform);
            head.transform.localPosition = new Vector3(0, 1.1f, 0);
            head.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
            head.GetComponent<Renderer>().material = MaterialLibrary.Player();
            Destroy(head.GetComponent<Collider>());

            GameObject beret = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beret.name = "Beret";
            beret.transform.SetParent(playerObj.transform);
            beret.transform.localPosition = new Vector3(0, 1.35f, 0);
            beret.transform.localScale    = new Vector3(0.42f, 0.18f, 0.42f);
            beret.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.1f, 0.4f, 0.1f));
            Destroy(beret.GetComponent<Collider>());

            AttachLimbs(playerObj.transform, MaterialLibrary.Solid(new Color(0.95f, 0.75f, 0.65f)));
            AttachSatchel(playerObj.transform);
        }

        CapsuleCollider col = playerObj.GetComponent<CapsuleCollider>();
        if (col == null) col = playerObj.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.3f;
        col.center = new Vector3(0, 0.9f, 0);

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) rb = playerObj.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        Animator anim = playerObj.GetComponent<Animator>();
        if (anim == null) anim = playerObj.AddComponent<Animator>();
        anim.applyRootMotion = false;
        if (playerController != null) anim.runtimeAnimatorController = playerController;

        if (playerObj.GetComponent<PlayerController>() == null) playerObj.AddComponent<PlayerController>();
        if (playerObj.GetComponent<PlayerInteraction>() == null) playerObj.AddComponent<PlayerInteraction>();
        if (playerObj.GetComponent<DistractionSystem>() == null) playerObj.AddComponent<DistractionSystem>();
        if (playerObj.GetComponent<PlayerVisibilityCulling>() == null) playerObj.AddComponent<PlayerVisibilityCulling>();
    }

    private void BuildEnemies()
    {
        int level = 1;
        if (GameManager.Instance != null) level = GameManager.Instance.currentLevel;

        // Lính gác tháp 1
        Transform[] p1 = CreateWaypoints("TowerGuard1", new Vector3(-12, 6.1f, -12));
        CreateEnemy("Guard_Tower1", new Vector3(-12, 6.1f, -12), p1);

        // Lính tuần tra 1
        Transform[] p3 = CreateWaypoints("Patrol_Village1", new Vector3(0, 0.9f, 0), new Vector3(10, 0.9f, -10), new Vector3(20, 0.9f, 0), new Vector3(10, 0.9f, 10));
        CreateEnemy("Patrol_1", new Vector3(5, 0.9f, -5), p3);

        if (level >= 2)
        {
            // Tăng cường ở Màn 2
            Transform[] p2 = CreateWaypoints("TowerGuard2", new Vector3(25, 6.1f, 15));
            CreateEnemy("Guard_Tower2", new Vector3(25, 6.1f, 15), p2);

            Transform[] p4 = CreateWaypoints("Patrol_Village2", new Vector3(-5, 0.9f, 15), new Vector3(5, 0.9f, 25), new Vector3(-5, 0.9f, 5));
            CreateEnemy("Patrol_2", new Vector3(-5, 0.9f, 15), p4);
        }

        if (level >= 3)
        {
            // Tăng cường đội tuần tra rừng ở Màn 3
            Transform[] p5 = CreateWaypoints("Patrol_River", new Vector3(-35, 0.9f, 0), new Vector3(-35, 0.9f, 20), new Vector3(-20, 0.9f, 35));
            CreateEnemy("Patrol_River1", new Vector3(-35, 0.9f, 0), p5);
            
            Transform[] p6 = CreateWaypoints("Patrol_River2", new Vector3(30, 0.9f, -30), new Vector3(10, 0.9f, -30), new Vector3(30, 0.9f, -10));
            CreateEnemy("Patrol_River2", new Vector3(30, 0.9f, -30), p6);
        }
    }

    private void BuildEscort()
    {
        GameObject escort;
        if (escortModel != null)
        {
            escort = Instantiate(escortModel);
            escort.name = "CanBo_Escort";
        }
        else
        {
            escort = new GameObject("CanBo_Escort");
        }
        escort.transform.position = new Vector3(0, 0.9f, 25);

        if (escortModel == null)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(escort.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(0.5f, 0.9f, 0.5f);
            body.GetComponent<Renderer>().material = MaterialLibrary.Escort();
            Destroy(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(escort.transform);
            head.transform.localPosition = new Vector3(0, 1.1f, 0);
            head.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
            head.GetComponent<Renderer>().material = MaterialLibrary.Escort();
            Destroy(head.GetComponent<Collider>());

            AttachLimbs(escort.transform, MaterialLibrary.Solid(new Color(0.9f, 0.72f, 0.6f)));
            CreateNonLa(escort.transform, new Vector3(0f, 1.35f, 0f), 0.55f, 0.22f);
        }

        CapsuleCollider col = escort.GetComponent<CapsuleCollider>();
        if (col == null) col = escort.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.3f;
        col.center = new Vector3(0, 0.9f, 0);

        NavMeshAgent agent = escort.GetComponent<NavMeshAgent>();
        if (agent == null) agent = escort.AddComponent<NavMeshAgent>();
        agent.height = 1.8f;
        
        Animator anim = escort.GetComponent<Animator>();
        if (anim == null) anim = escort.AddComponent<Animator>();
        if (escortController != null) anim.runtimeAnimatorController = escortController;

        if (escort.GetComponent<EscortTarget>() == null) escort.AddComponent<EscortTarget>();
    }

    private void BuildObjectives()
    {
        // Thư tình báo nhặt ngay đầu game
        GameObject letter;
        if (letterModel != null)
        {
            letter = Instantiate(letterModel);
            letter.name = "Letter_Pickup";
            letter.transform.position = new Vector3(-38, 0.5f, -42);
        }
        else
        {
            letter = new GameObject("Letter_Pickup");
            letter.transform.position = new Vector3(-38, 0.5f, -42);
            GameObject letterMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            letterMesh.transform.SetParent(letter.transform);
            letterMesh.transform.localScale = new Vector3(0.3f, 0.04f, 0.2f);
            letterMesh.GetComponent<Renderer>().material = MaterialLibrary.Letter();
            Destroy(letterMesh.GetComponent<Collider>());
        }

        BoxCollider lc = letter.GetComponent<BoxCollider>();
        if (lc == null) lc = letter.AddComponent<BoxCollider>();
        lc.isTrigger = true;
        lc.size      = new Vector3(1.2f, 1.2f, 1.2f); // slightly larger box for easier detection
        if (letter.GetComponent<LetterPickup>() == null) letter.AddComponent<LetterPickup>();

        // Trạm kết thúc (Cờ xanh)
        GameObject finish;
        if (flagModel != null)
        {
            finish = Instantiate(flagModel);
            finish.name = "Finish_Zone";
            finish.transform.position = new Vector3(45, 0, 45);
        }
        else
        {
            finish = new GameObject("Finish_Zone");
            finish.transform.position = new Vector3(45, 0, 45);
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flag.transform.SetParent(finish.transform);
            flag.transform.localPosition = new Vector3(0, 1f, 0);
            flag.transform.localScale    = new Vector3(0.1f, 1f, 0.1f);
            flag.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.4f, 0.3f, 0.15f));
            Destroy(flag.GetComponent<Collider>());
            GameObject flagTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagTop.transform.SetParent(finish.transform);
            flagTop.transform.localPosition = new Vector3(0.25f, 1.8f, 0);
            flagTop.transform.localScale    = new Vector3(0.5f, 0.3f, 0.05f);
            flagTop.GetComponent<Renderer>().material = MaterialLibrary.Checkpoint();
            Destroy(flagTop.GetComponent<Collider>());
        }

        SphereCollider sc = finish.GetComponent<SphereCollider>();
        if (sc == null) sc = finish.AddComponent<SphereCollider>();
        sc.radius    = 2.5f;
        sc.isTrigger = true;
        if (finish.GetComponent<FinishZoneTrigger>() == null) finish.AddComponent<FinishZoneTrigger>();

        // Checkpoint giữa màn
        GameObject cp = new GameObject("Checkpoint_Mid");
        cp.transform.position = new Vector3(0, 0, -25);
        GameObject cpMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cpMesh.transform.SetParent(cp.transform);
        cpMesh.transform.localPosition = new Vector3(0, 0.1f, 0);
        cpMesh.transform.localScale    = new Vector3(2f, 0.05f, 2f);
        cpMesh.GetComponent<Renderer>().material = MaterialLibrary.Checkpoint();
        Destroy(cpMesh.GetComponent<Collider>());
        SphereCollider cpc = cp.AddComponent<SphereCollider>();
        cpc.radius    = 1.5f;
        cpc.isTrigger = true;
        CheckpointTrigger cpt = cp.AddComponent<CheckpointTrigger>();
        SetPrivateField(cpt, "checkpointIndex", 1);
    }

    private void BuildLighting()
    {
        Light[] existing = FindObjectsByType<Light>(FindObjectsInactive.Exclude);
        foreach (Light l in existing) if (l.type == LightType.Directional) Destroy(l.gameObject);

        GameObject sun = new GameObject("Sun_Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type      = LightType.Directional;
        sunLight.color     = new Color(1f, 0.98f, 0.9f);  // Warm daylight
        sunLight.intensity = 2.5f;                          // Much brighter
        sunLight.shadows   = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

        // Bright ambient for jungle daytime
        RenderSettings.ambientMode        = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.55f, 0.72f, 0.55f);
        RenderSettings.ambientEquatorColor= new Color(0.45f, 0.55f, 0.35f);
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.30f, 0.18f);

        // Light fog for atmosphere
        RenderSettings.fog        = true;
        RenderSettings.fogMode    = FogMode.ExponentialSquared;
        RenderSettings.fogColor   = new Color(0.55f, 0.68f, 0.55f);
        RenderSettings.fogDensity = 0.008f; // Very light fog
    }

    private void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.backgroundColor = new Color(0.35f, 0.48f, 0.35f);
        cam.fieldOfView     = 65f;  
        cam.nearClipPlane   = 0.1f;
        cam.farClipPlane    = 150f;

        // Enable post-processing
        var urpData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (urpData != null) urpData.renderPostProcessing = true;

        CameraController cc = cam.GetComponent<CameraController>();
        if (cc == null) cc = cam.gameObject.AddComponent<CameraController>();
        if (playerObj != null) SetPrivateField(cc, "target", playerObj.transform);
        LayerMask camCollisionMask = LayerMask.GetMask("Default");
        SetPrivateField(cc, "collisionMask", camCollisionMask);

        if (FindAnyObjectByType<PostProcessingSetup>() == null)
        {
            GameObject ppObj = new GameObject("PostProcessing_Volume");
            ppObj.AddComponent<PostProcessingSetup>();
        }
    }

    private void SetupManagers()
    {
        if (GameManager.Instance == null) new GameObject("GameManager").AddComponent<GameManager>();
        
        if (AudioManager.Instance == null)
        {
            GameObject am = new GameObject("AudioManager");
            AudioSource musicSrc = am.AddComponent<AudioSource>();
            musicSrc.loop = true;
            musicSrc.volume = 0.5f;
            AudioSource sfxSrc = am.AddComponent<AudioSource>();
            sfxSrc.volume = 0.8f;
            AudioManager audioMgr = am.AddComponent<AudioManager>();
            SetPrivateField(audioMgr, "musicSource", musicSrc);
            SetPrivateField(audioMgr, "sfxSource",   sfxSrc);
        }
        if (CheckpointSystem.Instance == null) new GameObject("CheckpointSystem").AddComponent<CheckpointSystem>();
        
        if (ObjectiveManager.Instance == null)
        {
            GameObject om = new GameObject("ObjectiveManager");
            om.AddComponent<ObjectiveManager>();
        }
        
        ObjectiveManager.Instance.AddObjective("Nhặt thư tình báo tại cứ điểm xuất phát", ObjectiveManager.ObjectiveType.DeliverLetter);
        ObjectiveManager.Instance.AddObjective("Băng qua rừng hoặc trại giặc để tìm cán bộ", ObjectiveManager.ObjectiveType.EscortOfficial);
        ObjectiveManager.Instance.AddObjective("Dẫn cán bộ đến cứ điểm an toàn (Cờ Xanh)", ObjectiveManager.ObjectiveType.ReachCheckpoint);

        if (FindAnyObjectByType<GameHUD>() == null) new GameObject("GameHUD").AddComponent<GameHUD>();
        // Note: PauseMenuUI and GameOverUI are already on GameBootstrapper - no need for GameMenuUI
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CORE HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private void CreateEnemy(string name, Vector3 pos, Transform[] waypoints)
    {
        GameObject enemy;
        if (enemyModel != null)
        {
            enemy = Instantiate(enemyModel);
            enemy.name = name;
        }
        else
        {
            enemy = new GameObject(name);
        }
        enemy.transform.position = pos;

        if (enemyModel == null)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(enemy.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(0.5f, 0.9f, 0.5f);
            body.GetComponent<Renderer>().material = MaterialLibrary.Enemy();
            Destroy(body.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(enemy.transform);
            head.transform.localPosition = new Vector3(0, 1.1f, 0);
            head.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
            head.GetComponent<Renderer>().material = MaterialLibrary.Enemy();
            Destroy(head.GetComponent<Collider>());

            GameObject hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.transform.SetParent(enemy.transform);
            hat.transform.localPosition = new Vector3(0, 1.4f, 0);
            hat.transform.localScale    = new Vector3(0.45f, 0.08f, 0.45f);
            hat.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.3f, 0.25f, 0.15f));
            Destroy(hat.GetComponent<Collider>());

            AttachLimbs(enemy.transform, MaterialLibrary.Solid(new Color(0.9f, 0.72f, 0.6f)));
            AttachBackpack(enemy.transform);
            AttachRifle(enemy.transform);

            GameObject direction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            direction.transform.SetParent(enemy.transform);
            direction.transform.localPosition = new Vector3(0, 0.8f, 0.35f);
            direction.transform.localScale    = new Vector3(0.1f, 0.1f, 0.25f);
            direction.GetComponent<Renderer>().material = MaterialLibrary.Solid(Color.white);
            Destroy(direction.GetComponent<Collider>());
        }

        CapsuleCollider col = enemy.GetComponent<CapsuleCollider>();
        if (col == null) col = enemy.AddComponent<CapsuleCollider>();
        col.height = 1.8f;
        col.radius = 0.3f;
        col.center = new Vector3(0, 0.9f, 0);

        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        if (agent == null) agent = enemy.AddComponent<NavMeshAgent>();
        agent.height = 1.8f;
        agent.radius = 0.3f;
        agent.speed = 3.5f;

        Animator anim = enemy.GetComponent<Animator>();
        if (anim == null) anim = enemy.AddComponent<Animator>();
        if (enemyController != null) anim.runtimeAnimatorController = enemyController;

        GameObject canvasObj = new GameObject("DetectionCanvas");
        canvasObj.transform.SetParent(enemy.transform);
        canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

        GameObject gaugeBg = new GameObject("GaugeRoot");
        gaugeBg.transform.SetParent(canvasObj.transform);
        gaugeBg.transform.localPosition = Vector3.zero;
        gaugeBg.transform.localScale    = Vector3.one;
        RectTransform bgRect = gaugeBg.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 16);
        UnityEngine.UI.Image bgImg = gaugeBg.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0, 0, 0, 0.6f);

        GameObject gaugeFill = new GameObject("Fill");
        gaugeFill.transform.SetParent(gaugeBg.transform);
        gaugeFill.transform.localPosition = Vector3.zero;
        RectTransform fillRect = gaugeFill.AddComponent<RectTransform>();
        fillRect.sizeDelta = new Vector2(80, 16);
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        UnityEngine.UI.Image fillImg = gaugeFill.AddComponent<UnityEngine.UI.Image>();
        fillImg.color = Color.yellow;
        fillImg.type  = UnityEngine.UI.Image.Type.Filled;
        fillImg.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fillImg.fillAmount = 0f;

        DetectionGaugeUI gaugeUI = enemy.AddComponent<DetectionGaugeUI>();
        SetPrivateField(gaugeUI, "gaugeRoot", gaugeBg);
        SetPrivateField(gaugeUI, "fillImage", fillImg);

        if (enemy.GetComponent<EnemySoundDetection>() == null) enemy.AddComponent<EnemySoundDetection>();
        EnemyVision vision = enemy.GetComponent<EnemyVision>();
        if (vision == null) vision = enemy.AddComponent<EnemyVision>();
        LayerMask defaultMask = LayerMask.GetMask("Default");
        SetPrivateField(vision, "obstacleMask", defaultMask);
        SetPrivateField(vision, "playerMask", defaultMask);
        if (enemy.GetComponent<EnemyVisionCone>() == null) enemy.AddComponent<EnemyVisionCone>();
        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai == null) ai = enemy.AddComponent<EnemyAI>();
        ai.Waypoints = waypoints;
        ai.WaypointWaitTime = 2f;

        enemies.Add(enemy);
    }

    private Transform[] CreateWaypoints(string groupName, params Vector3[] positions)
    {
        GameObject group = new GameObject(groupName);
        Transform[] wps  = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject wp  = new GameObject($"WP_{i}");
            wp.transform.SetParent(group.transform);
            wp.transform.position = positions[i];
            wps[i] = wp.transform;
        }
        return wps;
    }

    private void CreateWall(Vector3 pos, Vector3 size, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.position   = pos;
        wall.transform.localScale = size;
        wall.GetComponent<Renderer>().material = MaterialLibrary.MountainRock();
        wall.layer = LayerMask.NameToLayer("Default");
    }

    private void CreateHidingZone(Vector3 pos, Vector3 size, string name)
    {
        GameObject zone = new GameObject(name);
        zone.transform.position = pos;
        zone.tag = "HidingZone";

        int bushCount = 8;
        for (int i = 0; i < bushCount; i++)
        {
            GameObject bush;
            if (bushModel != null)
            {
                bush = Instantiate(bushModel);
                bush.transform.SetParent(zone.transform);
                float rx = Random.Range(-size.x * 0.45f, size.x * 0.45f);
                float rz = Random.Range(-size.z * 0.45f, size.z * 0.45f);
                bush.transform.localPosition = new Vector3(rx, 0f, rz);
                float s = Random.Range(0.8f, 1.3f);
                bush.transform.localScale = new Vector3(s, s, s);
            }
            else
            {
                bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bush.transform.SetParent(zone.transform);
                float rx = Random.Range(-size.x * 0.45f, size.x * 0.45f);
                float rz = Random.Range(-size.z * 0.45f, size.z * 0.45f);
                bush.transform.localPosition = new Vector3(rx, 0.4f, rz);
                float s = Random.Range(0.6f, 1.1f);
                bush.transform.localScale    = new Vector3(s, s * 0.7f, s);
                bush.GetComponent<Renderer>().material = MaterialLibrary.Bush();
                Destroy(bush.GetComponent<Collider>());
            }
        }

        BoxCollider col = zone.AddComponent<BoxCollider>();
        col.size      = new Vector3(size.x, 2.0f, size.z);
        col.center    = new Vector3(0, 1.0f, 0);
        col.isTrigger = true;
    }

    private void CreateWaterZone(Vector3 pos, Vector3 size, string name = "River")
    {
        GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = name;
        river.transform.position   = pos;
        river.transform.localScale = size;
        river.GetComponent<Renderer>().material = MaterialLibrary.Water();
        river.tag = "WaterZone";

        Collider col = river.GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void SpawnTree(Vector3 pos)
    {
        if (treeModel != null)
        {
            GameObject tree = Instantiate(treeModel);
            tree.name = "Tree_3D";
            tree.transform.position = pos;
            tree.transform.localScale = Vector3.one * 1.5f;
            AddCollidersToModel(tree);
        }
        else
        {
            GameObject tree = new GameObject("Tree_Voxel");
            tree.transform.position = pos;

            // Thân cây (Cube)
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.transform.SetParent(tree.transform);
            trunk.transform.localPosition = new Vector3(0, 1.5f, 0);
            trunk.transform.localScale    = new Vector3(0.5f, 3f, 0.5f);
            trunk.GetComponent<Renderer>().material = MaterialLibrary.TreeTrunk();
            
            BoxCollider tc = trunk.GetComponent<BoxCollider>();
            if (tc) { tc.size = new Vector3(1f, 1f, 1f); } // Collider bao quanh thân

            // Tán lá Voxel (ghép từ nhiều cube ngẫu nhiên)
            int leafCount = Random.Range(10, 16);
            for(int i = 0; i < leafCount; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.transform.SetParent(tree.transform);
                
                float h = Random.Range(2.5f, 4.5f);
                float rad = Random.Range(0.2f, 1.5f);
                float angle = Random.Range(0, 360f);
                float lx = Mathf.Cos(angle) * rad;
                float lz = Mathf.Sin(angle) * rad;

                leaf.transform.localPosition = new Vector3(lx, h, lz);
                
                float size = Random.Range(1.2f, 2f);
                leaf.transform.localScale = new Vector3(size, size, size);
                leaf.transform.localRotation = Quaternion.Euler(Random.Range(0,90), Random.Range(0,90), Random.Range(0,90));
                
                leaf.GetComponent<Renderer>().material = MaterialLibrary.Tree();
                Destroy(leaf.GetComponent<Collider>());
            }
        }
    }

    private void CreateInteractProp(Vector3 pos, PlayerInteraction.DisguiseType type, string name)
    {
        GameObject prop = new GameObject(name);
        prop.transform.position = pos;
        prop.tag = "InteractPoint";

        GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mesh.transform.SetParent(prop.transform);
        mesh.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
        mesh.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.7f, 0.5f, 0.2f));
        Destroy(mesh.GetComponent<Collider>());

        BoxCollider col = prop.AddComponent<BoxCollider>();
        col.size      = new Vector3(1.5f, 1.5f, 1.5f);
        col.isTrigger = true;

        InteractPoint ip = prop.AddComponent<InteractPoint>();
        ip.disguiseType = type;
    }

    private void CreateBirdFlock(Vector3 groundPos)
    {
        GameObject flock = new GameObject("BirdFlock");
        flock.transform.position = groundPos;
        flock.AddComponent<BirdFlock>();
    }

    private void SpawnBamboo(Vector3 pos)
    {
        if (bambooModel != null)
        {
            GameObject bamboo = Instantiate(bambooModel);
            bamboo.name = "Bamboo_Grove_3D";
            bamboo.transform.position = pos;
            bamboo.transform.localScale = Vector3.one * 1.5f;
            AddCollidersToModel(bamboo);
        }
        else
        {
            GameObject group = new GameObject("Bamboo_Grove");
            group.transform.position = pos;

            int stemCount = 6;
            for (int j = 0; j < stemCount; j++)
            {
                GameObject stem = new GameObject($"Bamboo_{j}");
                stem.transform.SetParent(group.transform);
                stem.transform.localPosition = new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));
                
                float scaleY = Random.Range(4f, 6f);
                float radius = Random.Range(0.05f, 0.08f);

                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.transform.SetParent(stem.transform, false);
                seg.transform.localPosition = new Vector3(0f, scaleY * 0.5f, 0f);
                seg.transform.localScale = new Vector3(radius * 2f, scaleY * 0.5f, radius * 2f);
                seg.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.18f, 0.48f, 0.15f));
                
                CapsuleCollider cc = seg.GetComponent<CapsuleCollider>();
                if (cc) { cc.height = scaleY; cc.radius = radius; }

                GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leaves.transform.SetParent(stem.transform, false);
                leaves.transform.localPosition = new Vector3(0f, scaleY + 0.2f, 0f);
                leaves.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
                leaves.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.25f, 0.55f, 0.2f));
                Destroy(leaves.GetComponent<Collider>());
            }
        }
    }

    private void CreateFence(Vector3 start, Vector3 end)
    {
        if (fenceModel != null)
        {
            GameObject fenceGroup = new GameObject("Fence_Line");
            fenceGroup.transform.position = start;

            Vector3 dir = end - start;
            float dist = dir.magnitude;
            Vector3 step = dir.normalized * 2f;
            int posts = Mathf.CeilToInt(dist / 2f);

            for (int i = 0; i <= posts; i++)
            {
                Vector3 pos = start + step * i;
                if (Vector3.Distance(start, pos) > dist) pos = end;

                GameObject post = Instantiate(fenceModel);
                post.transform.SetParent(fenceGroup.transform);
                post.transform.position = pos;
                
                if (dir.sqrMagnitude > 0.01f)
                    post.transform.rotation = Quaternion.LookRotation(dir);

                post.transform.localScale = Vector3.one * 1.5f;
                AddCollidersToModel(post);
            }
        }
        else
        {
            GameObject fenceGroup = new GameObject("Fence_Line");
            fenceGroup.transform.position = start;

            Vector3 dir = end - start;
            float dist = dir.magnitude;
            Vector3 step = dir.normalized * 2f;
            int posts = Mathf.CeilToInt(dist / 2f);

            for (int i = 0; i <= posts; i++)
            {
                Vector3 pos = start + step * i;
                if (Vector3.Distance(start, pos) > dist) pos = end;

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.SetParent(fenceGroup.transform);
                post.transform.position = pos + Vector3.up * 0.6f;
                post.transform.localScale = new Vector3(0.08f, 0.6f, 0.08f);
                post.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();
                
                if (i < posts)
                {
                    GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bar.transform.SetParent(fenceGroup.transform);
                    Vector3 nextPos = start + step * (i + 1);
                    if (Vector3.Distance(start, nextPos) > dist) nextPos = end;

                    bar.transform.position = (pos + nextPos) * 0.5f + Vector3.up * 0.8f;
                    Vector3 barDir = nextPos - pos;
                    bar.transform.rotation = Quaternion.LookRotation(barDir);
                    bar.transform.localScale = new Vector3(0.04f, 0.04f, barDir.magnitude);
                    bar.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();
                    Destroy(bar.GetComponent<Collider>());
                }
            }
        }
    }

    private void SpawnLantern(Vector3 pos)
    {
        GameObject lantern;
        if (lanternModel != null)
        {
            lantern = Instantiate(lanternModel);
            lantern.name = "Lantern_3D";
            lantern.transform.position = pos;
            lantern.transform.localScale = Vector3.one * 1.2f;
        }
        else
        {
            lantern = new GameObject("Lantern");
            lantern.transform.position = pos;

            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(lantern.transform);
            post.transform.localPosition = new Vector3(0, 1.2f, 0);
            post.transform.localScale = new Vector3(0.08f, 1.2f, 0.08f);
            post.GetComponent<Renderer>().material = MaterialLibrary.WoodPlank();

            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.transform.SetParent(lantern.transform);
            glass.transform.localPosition = new Vector3(0.3f, 2.2f, 0);
            glass.transform.localScale = new Vector3(0.2f, 0.3f, 0.2f);
            glass.GetComponent<Renderer>().material = MaterialLibrary.LanternGlow();
            Destroy(glass.GetComponent<Collider>());
        }

        GameObject lightObj = new GameObject("PointLight");
        lightObj.transform.SetParent(lantern.transform);
        lightObj.transform.localPosition = new Vector3(0, 2f, 0);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.75f, 0.3f);
        l.range = 10f;
        l.intensity = 3f;
    }

    private void AttachLimbs(Transform parent, Material skinMat)
    {
        GameObject armL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        armL.transform.SetParent(parent, false);
        armL.transform.localPosition = new Vector3(-0.35f, 0.2f, 0f);
        armL.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        armL.GetComponent<Renderer>().material = skinMat;
        Destroy(armL.GetComponent<Collider>());

        GameObject armR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        armR.transform.SetParent(parent, false);
        armR.transform.localPosition = new Vector3(0.35f, 0.2f, 0f);
        armR.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
        armR.GetComponent<Renderer>().material = skinMat;
        Destroy(armR.GetComponent<Collider>());

        GameObject legL = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        legL.transform.SetParent(parent, false);
        legL.transform.localPosition = new Vector3(-0.15f, -0.6f, 0f);
        legL.transform.localScale = new Vector3(0.15f, 0.35f, 0.15f);
        legL.GetComponent<Renderer>().material = skinMat;
        Destroy(legL.GetComponent<Collider>());

        GameObject legR = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        legR.transform.SetParent(parent, false);
        legR.transform.localPosition = new Vector3(0.15f, -0.6f, 0f);
        legR.transform.localScale = new Vector3(0.15f, 0.35f, 0.15f);
        legR.GetComponent<Renderer>().material = skinMat;
        Destroy(legR.GetComponent<Collider>());
    }

    private void AttachRifle(Transform parent)
    {
        GameObject rifle = new GameObject("Rifle");
        rifle.transform.SetParent(parent, false);
        rifle.transform.localPosition = new Vector3(0.35f, 0.2f, 0.35f);
        rifle.transform.localRotation = Quaternion.Euler(20f, 180f, 15f);

        GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stock.transform.SetParent(rifle.transform, false);
        stock.transform.localScale = new Vector3(0.06f, 0.1f, 0.6f);
        stock.GetComponent<Renderer>().material = MaterialLibrary.RifleWood();
        Destroy(stock.GetComponent<Collider>());

        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.transform.SetParent(rifle.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0.04f, 0.45f);
        barrel.transform.localScale = new Vector3(0.025f, 0.35f, 0.025f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.GetComponent<Renderer>().material = MaterialLibrary.RifleMetal();
        Destroy(barrel.GetComponent<Collider>());
    }

    private void AttachSatchel(Transform parent)
    {
        GameObject bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bag.transform.SetParent(parent, false);
        bag.transform.localPosition = new Vector3(-0.15f, 0.2f, -0.22f);
        bag.transform.localScale = new Vector3(0.32f, 0.24f, 0.08f);
        bag.transform.localRotation = Quaternion.Euler(5f, -10f, 15f);
        bag.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.42f, 0.32f, 0.22f));
        Destroy(bag.GetComponent<Collider>());
    }

    private void AttachBackpack(Transform parent)
    {
        GameObject pack = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pack.transform.SetParent(parent, false);
        pack.transform.localPosition = new Vector3(0f, 0.3f, -0.24f);
        pack.transform.localScale = new Vector3(0.38f, 0.45f, 0.18f);
        pack.GetComponent<Renderer>().material = MaterialLibrary.Solid(new Color(0.32f, 0.28f, 0.2f));
        Destroy(pack.GetComponent<Collider>());
    }

    private GameObject CreateNonLa(Transform parent, Vector3 localPos, float radius, float height)
    {
        GameObject nonLa = new GameObject("NonLa");
        nonLa.transform.SetParent(parent, false);
        nonLa.transform.localPosition = localPos;

        MeshFilter mf = nonLa.AddComponent<MeshFilter>();
        MeshRenderer mr = nonLa.AddComponent<MeshRenderer>();
        mr.material = MaterialLibrary.NonLaStraw();

        Mesh mesh = new Mesh();
        int segments = 24;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];

        vertices[0] = new Vector3(0, height * 0.5f, 0);
        vertices[segments + 1] = new Vector3(0, -height * 0.5f, 0);

        for (int i = 0; i < segments; i++)
        {
            float angle = ((float)i / segments) * 2f * Mathf.PI;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, -height * 0.5f, Mathf.Sin(angle) * radius);
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;
            triangles[i * 6] = 0;
            triangles[i * 6 + 1] = i + 1;
            triangles[i * 6 + 2] = next + 1;
            triangles[i * 6 + 3] = segments + 1;
            triangles[i * 6 + 4] = next + 1;
            triangles[i * 6 + 5] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        return nonLa;
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private void BakeNavMeshRuntime()
    {
        NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
        if (surface != null) {
            surface.BuildNavMesh();
        } else {
            NavMeshSurface nmSurf = new GameObject("NavMesh").AddComponent<NavMeshSurface>();
            nmSurf.collectObjects  = CollectObjects.All;
            nmSurf.useGeometry     = NavMeshCollectGeometry.PhysicsColliders;
            nmSurf.BuildNavMesh();
        }
    }

    private void AddCollidersToModel(GameObject go)
    {
        MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
        foreach (var r in renderers)
        {
            if (r.gameObject.GetComponent<Collider>() == null)
            {
                MeshFilter mf = r.gameObject.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = r.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }
}

/// <summary>
/// Trigger vùng kết thúc màn - gắn vào cờ đích.
/// </summary>
public class FinishZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("[Level] Kim Đồng đã đến trạm an toàn!");
        ObjectiveManager.Instance?.CompleteCurrentObjective();
    }
}
