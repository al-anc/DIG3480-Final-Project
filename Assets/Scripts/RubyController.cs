using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RubyController : MonoBehaviour
{
    public float speed = 3.0f;

    public Text scoreText;
    public Text cogText;
    public Text winText;
    public Text GameOverText;

    public int maxHealth = 5;
    public static int jambiTalkCount;

    public GameObject projectilePrefab;
    public GameObject OwPrefab;
    public GameObject HealthPrefab;

    public GameObject timeDisplay;
    public int secondsLeft = 25;
    public bool takingAway = false;

    public AudioClip throwSound;
    public AudioClip hitSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    public AudioClip robotFix;
    public AudioClip wooshSound;
    AudioSource audioSource;

    public float timeInvincible = 2.0f;

    public int health { get { return currentHealth; } }
    public int score { get { return currentScore; } }
    public int currentScene;
    int currentHealth;
    int currentCog;
    int currentScore = 0;

    private float currentSpeed;
    public float dashSpeed = 10f;

    public float currentDashTimer;
    public float startDashTimer = 5f;
    public bool isDashing = false;
    public bool canDash = true;

    bool gameOver;
    bool isInvincible;
    float invincibleTimer;

    Rigidbody2D rigidbody2d;
    float horizontal;
    float vertical;

    Animator animator;
    Vector2 lookDirection = new Vector2(1, 0);

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        currentSpeed = speed;

        currentHealth = maxHealth;
        currentCog = 2;
        GameOverText.text = "";

        scoreText.text = currentScore + "/5 Robots fixed";
        cogText.text = "" + currentCog;

        timeDisplay.GetComponent<Text>().text = "";

        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {

        HandleDash();

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector2 move = new Vector2(horizontal, vertical);

        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
        }

        if (takingAway == false && secondsLeft > 0 && jambiTalkCount == 1 && currentScene == 0 && currentScore != 5 && currentHealth != 0)
        {
            StartCoroutine(TimerTake());
        }

        if (takingAway == false && secondsLeft > 0 && jambiTalkCount == 2 && currentScene == 1 && currentScore != 5 && currentHealth != 0)
        {
            StartCoroutine(TimerTake());
        }

        if (currentScene == 0 && currentScore == 0)
        {
            winText.text = "Press 'E' to Talk to Jambi for controls and time attack";
        }

        if (currentScene == 0 && currentScore != 0 || jambiTalkCount == 1)
        {
            winText.text = "";
        }

        if (currentScene == 1)
        {
            winText.text = "";
        }

        animator.SetFloat("Look X", lookDirection.x);
        animator.SetFloat("Look Y", lookDirection.y);
        animator.SetFloat("Speed", move.magnitude);

        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
                isInvincible = false;
        }

        if (currentScore == 5 && secondsLeft > 0 && currentScene == 0)
        {
            currentScene = SceneManager.GetActiveScene().buildIndex;
            winText.text = "Talk to Jambi to visit stage two!";
        }

        if (currentScore == 5 && secondsLeft > 0 && currentScene == 1)
        {
            winText.text = "You Win! Game created by Alan Castro";
            jambiTalkCount = 0;
            if (Input.GetKey(KeyCode.R))
            {
                SceneManager.LoadScene("FirstScene");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && currentCog != 0)
        {
            Launch();
        }

        if (currentHealth == 0)
        {
            GameOverText.text = "You lost! Press R to restart";
            winText.text = "";
            currentSpeed = 0;
            speed = 0;

            if (jambiTalkCount == 1)
            {
                jambiTalkCount = 0;
            }

            gameOver = true;
        }

        if (secondsLeft == 0)
        {
            GameOverText.text = "Times up! Press R to restart";
            winText.text = "";
            currentSpeed = 0;
            speed = 0;

            if (jambiTalkCount == 1)
            {
                jambiTalkCount = 0;
            }

            gameOver = true;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit2D hit = Physics2D.Raycast(rigidbody2d.position + Vector2.up * 0.2f, lookDirection, 1.5f, LayerMask.GetMask("NPC"));
            if (hit.collider != null)
            {
                if (jambiTalkCount == 0 && currentScore == 0)
                {
                    StartCoroutine(TimerTake());
                    timeDisplay.GetComponent<Text>().text = "00:" + secondsLeft;
                    jambiTalkCount += 1;
                }

                if (jambiTalkCount == 1 && currentScore == 5)
                {
                    jambiTalkCount += 1;
                }

                if (currentScore == 5)
                {
                    NonPlayerCharacter character = hit.collider.GetComponent<NonPlayerCharacter>();
                    if (character != null)
                    {
                        SceneManager.LoadScene("SecondScene");
                    }
                }

                else
                {
                    NonPlayerCharacter character = hit.collider.GetComponent<NonPlayerCharacter>();
                    if (character != null)
                    {
                        winText.text = "";
                        character.DisplayDialog();
                    }
                }
            }
        }

        if (Input.GetKey(KeyCode.R))
        {
            if (gameOver == true)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }

    void FixedUpdate()
    {
        HandleInput();

        Vector2 position = rigidbody2d.position;
        position.x = position.x + currentSpeed * horizontal * Time.deltaTime;
        position.y = position.y + currentSpeed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(position);
    }

    void HandleInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector2 currentPos = rigidbody2d.position;

        currentPos.x = currentPos.x + currentSpeed * horizontal * Time.deltaTime;
        currentPos.y = currentPos.y + currentSpeed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(currentPos);
    }

    public void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            isDashing = true;
            canDash = false;
            currentDashTimer = startDashTimer;
        }

        if (isDashing)
        {
            currentSpeed = dashSpeed;
            currentDashTimer -= Time.deltaTime;

            if (currentDashTimer <= 0)
            {
                isDashing = false;
                canDash = true;
                PlaySound(wooshSound);
                currentSpeed = speed;
            }
        }
    }

    public void ChangeHealth(int amount)
    {
        if (amount < 0)
        {
            animator.SetTrigger("Hit");
            if (isInvincible)
                return;

            isInvincible = true;
            invincibleTimer = timeInvincible;

            PlaySound(hitSound);
            GameObject projectileObject = Instantiate(OwPrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);
        }

        if (amount > 0)
        {
            GameObject projectileObject = Instantiate(HealthPrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);
        }

        if (currentHealth == 1)
        {
            audioSource.clip = loseSound;
            audioSource.loop = false;
            audioSource.Play();
        }

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIHealthBar.instance.SetValue(currentHealth / (float)maxHealth);
    }

    public void ChangeCog(int amount)
    {
        currentCog = currentCog + amount;
        cogText.text = "" + currentCog;
    }

    public void ChangeScore()
    {
        currentScore = currentScore + 1;
        scoreText.text = currentScore + "/5 Robots fixed";

        PlaySound(robotFix);

        if (currentScore == 5)
        {
            audioSource.clip = winSound;
            audioSource.loop = false;
            audioSource.Play();
        }
    }

    void Launch()
    {
        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(lookDirection, 300);

        animator.SetTrigger("Launch");

        currentCog = currentCog - 1;
        cogText.text = "" + currentCog;

        PlaySound(throwSound);
    }

    IEnumerator TimerTake()
    {
        takingAway = true;
        yield return new WaitForSeconds(1);
        secondsLeft -= 1;
        if (secondsLeft < 10)
        {
            timeDisplay.GetComponent<Text>().text = "00:0" + secondsLeft;
        }
        else
        {
            timeDisplay.GetComponent<Text>().text = "00:" + secondsLeft;
        }

        if (secondsLeft == 0)
        {
            audioSource.clip = loseSound;
            audioSource.loop = false;
            audioSource.Play();
        }

        takingAway = false;
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}