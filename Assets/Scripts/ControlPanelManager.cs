using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;
using System.Collections;

public class ControlPanelManager : MonoBehaviour
{
    public GameObject submarine;
    public GameObject submarineModel;
    public Material underWaterSkybox;
    public Material aboveWaterSkybox;
    public TextMeshProUGUI depthText;
    public TextMeshProUGUI TopPanelText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI GameOverText;
    Camera camera;
    public AudioSource damageSound;
    public AudioSource chokingSound;
    public AudioSource explosionSound;
    public GameObject Window;
    public Material GameOverMaterial;
    public Slider slider;
    public GameObject FadeCube;
    public GameObject ResetButton;
    Rigidbody submarineRB;
    bool takingDamage = false;
    float health = 100f;
    float currentSpinSpeed;
    float currentSubSpeed = 0f;
    float currentAscendSpeed = 0f;
    float currentOxygen = 100f;
    float score = 0;
    public float oyxgenDepletionRate = 1f;
    public float subSpeed = 2f;
    public float ascendSpeed = 1f;
    public float spinSpeed = 15f;
    private bool isGameOver = false;

    void Awake()
    {
        camera = Camera.main;
        submarineRB = submarine.GetComponent<Rigidbody>();
    }

    void Start()
    {
        StartCoroutine(DamageRoutine());
    }

    void Update()
    {   
        depthText.text = $"Depth: {102 - submarine.transform.position.y * 2f:0.0}m";
        TopPanelText.text = $"${score}";
        healthText.text = $"Hull%: {health}";

        if (currentOxygen <= 10f)
        {
            Material FadeMat = FadeCube.GetComponent<MeshRenderer>().sharedMaterial;
            Color c = FadeMat.color;
            c.a = 0.5f + 0.5f * (1f - currentOxygen / 10f);
            FadeMat.color = c;
            if (!chokingSound.isPlaying)
            {
                chokingSound.Play();
            }
        }
        else if (currentOxygen > 10f)
        {
            Material FadeMat = FadeCube.GetComponent<MeshRenderer>().sharedMaterial;
            Color c = FadeMat.color;
            c.a = 0f;
            FadeMat.color = c;
            chokingSound.Stop();
        }
        if (health <= 0f || currentOxygen <= 0f)
        {
            if (!isGameOver)
            {
                explosionSound.Play();
                isGameOver = true;
            }
            Window.GetComponent<MeshRenderer>().material = GameOverMaterial;
            chokingSound.Stop();
            FadeCube.SetActive(false);
            GameOverText.gameObject.SetActive(true);
            ResetButton.SetActive(true);
        }
        if (isGameOver) return;
        submarineRB.linearVelocity = Vector3.zero;
        submarineRB.angularVelocity = Vector3.zero;

        if (submarine.transform.position.y > 50f)
        {
            RenderSettings.skybox = aboveWaterSkybox;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 1000f;
            camera.cullingMask &= ~(1 << LayerMask.NameToLayer("Wall"));
            DynamicGI.UpdateEnvironment();
        }
        else
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0.04894827f, 0.6981132f, 0.01960784f);
            camera.farClipPlane = 15f;
            camera.cullingMask |= 1 << LayerMask.NameToLayer("Wall");
            RenderSettings.skybox = underWaterSkybox;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.reflectionIntensity = 0f;
            RenderSettings.fog = true;
            DynamicGI.UpdateEnvironment();
        }

        if (submarine.transform.position.y > 50f)
        {
            currentOxygen += Time.deltaTime * oyxgenDepletionRate * 2f;
        }
        else
        {
            currentOxygen -= Time.deltaTime * oyxgenDepletionRate;
        }
        
        if (currentOxygen < 0f) currentOxygen = 0f;
        if (currentOxygen > 100f) currentOxygen = 100f;
        slider.value = currentOxygen;

        submarineRB.angularVelocity = Vector3.up * currentSpinSpeed * Mathf.Deg2Rad;

        Vector3 moveDirection = submarine.transform.right * currentSubSpeed;
        moveDirection.y = currentAscendSpeed;
        submarineRB.linearVelocity = moveDirection;

        submarineModel.transform.position = submarine.transform.position;
        submarineModel.transform.rotation = submarine.transform.rotation;
    }

    void FixedUpdate()
    {
        Vector3 pos = submarine.transform.position;
        if (pos.y < 3f)
        {
            pos.y = 3f;
            submarine.transform.position = pos;
        }
        if (pos.y > 51f)
        {
            pos.y = 51f;
            submarine.transform.position = pos;
        }
    }

    public void Rotate(float speed)
    {
        currentSpinSpeed = spinSpeed * speed; 
    }

    public void ForwardBackward(float speed)
    {
        currentSubSpeed = subSpeed * speed;
    }

    public void AscendDescend(float speed)
    {
        currentAscendSpeed = ascendSpeed * speed;
    }

    public void CollectTreasure(float value)
    {
        Debug.Log("Adding " + value + " to score.");
        score += value;
    }

    public void OnDamageCollision(bool value)
    {
        takingDamage = value;
    }

    IEnumerator DamageRoutine()
    {
        while (true)
        {
            if (takingDamage && !isGameOver)
            {
                damageSound.mute = false;
                health -= 2f;
                health = Mathf.Clamp(health, 0f, 100f);
                yield return new WaitForSeconds(1f);
            }
            else
            {
                damageSound.mute = true;
                yield return null;
            }
        }
    }

    public void OnRepair()
    {
        health = Mathf.Clamp(health + 5f, 0, 100);
    }
 }
