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
    public int maxScore = 4;

    public GameObject projectilePrefab;
    public GameObject OwPrefab;
    public GameObject HealthPrefab;

    public AudioClip throwSound;
    public AudioClip hitSound;
    public AudioClip winSound;
    public AudioClip loseSound;
    AudioSource audioSource;

    public float timeInvincible = 2.0f;

    public int health { get { return currentHealth; } }
    public int score { get { return currentScore; } }
    public int currentScene;
    int currentHealth;
    int currentCog;
    int currentScore = 0;

    bool isInvincible;
    float invincibleTimer;

    bool gameOver;

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

        currentHealth = maxHealth;
        currentCog = 2;
        winText.text = "";
        GameOverText.text = "";

        scoreText.text = "Score: " + currentScore;
        cogText.text = "" + currentCog;

        audioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        Vector2 move = new Vector2(horizontal, vertical);

        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
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

        if (currentScore == 4)
        {
            currentScene = SceneManager.GetActiveScene().buildIndex;
            if (currentScene == 0)
                winText.text = "Talk to Jambi to visit stage two!";
        }

        if (currentScore == 5)
        {
            winText.text = "You Win! Game created by Alan Castro";
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
            speed = 0;

            gameOver = true;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            RaycastHit2D hit = Physics2D.Raycast(rigidbody2d.position + Vector2.up * 0.2f, lookDirection, 1.5f, LayerMask.GetMask("NPC"));
            if (hit.collider != null)
            {
                if (currentScore == 4)
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
        Vector2 position = rigidbody2d.position;
        position.x = position.x + speed * horizontal * Time.deltaTime;
        position.y = position.y + speed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(position);
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
        scoreText.text = "Score: " + currentScore;

        if (currentScore == 4)
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

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }
}