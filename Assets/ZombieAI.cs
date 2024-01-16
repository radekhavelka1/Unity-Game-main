using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class ZombieAI : MonoBehaviour
{
    public GameObject Target;
    public float speed = 0.5f;
    public float detectionRadius = 5.0f;
    public float chaseRadius = 5.0f;
    public float attackRadius = 2.0f;
    public PlayerMovement player;
    public HealthBar healthBar;

    public Transform groundCheck_Zombie;
    public float groundDistance = 0.5f;
    public LayerMask groundMask;
    public LayerMask detectionMask;

    private bool isGrounded;
    private Rigidbody rb;
    private Animator animator;
    public Text healthText;
    private int currentHealth;

    public bool idle = true;
    public bool run = false;

    private float attackPeriod = 2f;
    private int attackDamage = 10;

    private float timeSinceLastAttack = 0f;

    public int zombieHealth;

    private UnityEngine.AI.NavMeshAgent navMeshAgent;


    public Transform[] patrolWaypoints;
    private int currentWaypointIndex = 0;


    private Vector3 currentTargetPosition;

    private bool chasing;
    public bool isDead = false;
    private float patrolTimer = 0f;
    private float idleTimer = 0f;
    private bool isIdling = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        currentHealth = player.Health;
        healthBar.SetMaxHealth(currentHealth);
        UpdateHealthText();
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

    }

    private void Update()
    {

        isGrounded = Physics.CheckSphere(groundCheck_Zombie.position, groundDistance, groundMask);

        if (!isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, groundMask))
            {
                transform.position = hit.point + Vector3.up * groundDistance;
            }
        }

        if (zombieHealth == 0)
        {
            isDead = true;
            Debug.Log(zombieHealth);
            rb.velocity = Vector3.zero;
            animator.SetBool("Attack", false);
            animator.SetBool("Dead", true);

            StartCoroutine(DisappearAfterDeadAnimation());
        }

        float distanceToTarget = Vector3.Distance(transform.position, Target.transform.position);
        
        if (distanceToTarget <= chaseRadius )
        {
            chaseRadius = 15.0f;
            if (distanceToTarget <= attackRadius && !isDead)
            {
                // Stop moving and play the attack animation
                rb.velocity = Vector3.zero;
                transform.LookAt(Target.transform);

                animator.SetBool("Attack", true);
                animator.SetBool("Idle", false);
                animator.SetBool("RUN", false);

                timeSinceLastAttack += Time.deltaTime;

                if (timeSinceLastAttack >= attackPeriod)
                {
                    Attack();
                    timeSinceLastAttack = 0f;
                }
            }
          
            else if (!isDead)
            {
                transform.LookAt(Target.transform);
                if (Vector3.Distance(currentTargetPosition, Target.transform.position) > 1.0f)
                {
                    currentTargetPosition = Target.transform.position;
                    navMeshAgent.SetDestination(currentTargetPosition);
                    
                }

                // Play the run animation
                animator.SetBool("Attack", false);
                animator.SetBool("Idle", false);
                animator.SetBool("RUN", true);
            }
        }

        else
        {
            Patrol();

        }
    }

    private IEnumerator DisappearAfterDeadAnimation()
    {
        // Wait for the dead animation to complete (you might need to adjust the time)
        yield return new WaitForSeconds(2f); // Change this value based on your animation length

        // Do any logic to make the zombie disappear
        gameObject.SetActive(false); // Or you can destroy the GameObject if needed
    }


    // Draw the detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }

    private void Attack()
    {
        // Apply damage to the target
        player.Health -= attackDamage;
        healthBar.SetHealth(player.Health);
        currentHealth = player.Health;
        if (currentHealth <= 0)
        {
            SceneManager.LoadScene("GameOver");
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        UpdateHealthText();
    }

    private void UpdateHealthText()
    {
        // Update the text object with the player's current health
        healthText.text = "HP: " + currentHealth.ToString();
    }


    private void Patrol()
    {
        float distanceToTarget = Vector3.Distance(transform.position, Target.transform.position);

        float angleToPlayer = Vector3.Angle(transform.forward, (Target.transform.position - transform.position).normalized);

        if (angleToPlayer > 90.0f)
        {
            chaseRadius = 1.0f;

            if (distanceToTarget < chaseRadius)
            {
                return;
            }
        }
        else
        {
            chaseRadius = 15.0f;
        }

        // Increment the patrol timer
        patrolTimer += Time.deltaTime;

        // Check if it's time to idle
        if (patrolTimer >= 5.0f && !isIdling)
        {
            isIdling = true;
            idleTimer = 0f;

            // Stop patrolling and play the idle animation
            animator.SetBool("RUN", false);
            animator.SetBool("Walk", true);
        }

        // Check if it's time to resume patrolling after idling for 2 seconds
        if (isIdling)
        {
            idleTimer += Time.deltaTime;

            if (idleTimer >= 2.0f)
            {
                isIdling = false;
                patrolTimer = 0f;
                animator.SetBool("Walk", false);
                animator.SetBool("RUN", true);
            }
        }

        // Zkontrolujte, zda dos�hli aktu�ln�ho patroln�ho bodu
        if (Vector3.Distance(transform.position, patrolWaypoints[currentWaypointIndex].position) < 1.0f)
        {
            // P�ejd�te na dal�� bod v seznamu
            currentWaypointIndex = (currentWaypointIndex + 1) % patrolWaypoints.Length;
        }

        // Nastavte destinaci zombika na aktu�ln� patroln� bod
        navMeshAgent.SetDestination(patrolWaypoints[currentWaypointIndex].position);

        // Oto�en� sm�rem k patroln�mu bodu
        Vector3 targetDirection = (patrolWaypoints[currentWaypointIndex].position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(targetDirection.x, 0, targetDirection.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);  // �prava rychlosti ot��en�
        transform.LookAt(patrolWaypoints[currentWaypointIndex].position);

        // Ov��te, zda zombik dos�hl patroln�ho bodu
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            // Zombik dos�hl patroln�ho bodu, tak�e m��ete zde prov�st n�jakou akci
            // Nap��klad m��ete zm�nit animaci nebo prov�st jin� �koly
        }
    }

}