using System.Linq;
using UnityEngine;
using UnityEngine.AI;

using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>TankSM</c> state machine for the tank.
    /// </summary>
    internal class TankSM : StateMachine
    {
        protected internal struct States
        {
            // States:
            public PatrollingState Patrolling;
            public HidingState Hiding;
            public GettingCenterState GettingCenter;
            public ChasingState Chasing;
            public RangeFindingState RangeFinding;
            public EvadingState Evading;

            internal States(TankSM sm)
            {
                Patrolling = new PatrollingState(sm);
                Hiding = new HidingState(sm);
                GettingCenter = new GettingCenterState(sm);
                Chasing = new ChasingState(sm);
                Evading = new EvadingState(sm);
                RangeFinding = new RangeFindingState(sm);
            }
        }

        public States m_States;
        [HideInInspector] public GameManager GameManager;           // Reference to the GameManager.
        [HideInInspector] public NavMeshAgent NavMeshAgent;         // Reference to the NavMeshAgent.
        [Header("Patrolling")]
        [Tooltip("Minimum and maximum time delay for patrolling wait.")]
        public Vector2 PatrolWaitTime = new(1.5f, 3.5f);            // A minimum and maximum time delay for patrolling wait.
        [Tooltip("Minimum and maximum circumradius of the area to patrol at a given update time.")]
        public Vector2 PatrolMaxDist = new(15f, 30f);               // A minimum and maximum circumradius of the area to patrol.
        [Range(0f, 2f)] public float PatrolNavMeshUpdate = 0.2f;    // A delay between each parolling path update.
        [Header("Targeting")]
        [Tooltip("Minimum and maximum range for the targeting range.")]
        public Vector2 StartToTargetDist = new(28f, 35f);           // A minimum and maximum range for the targeting range.
        [HideInInspector] public float TargetDistance;              // The distance between the tank and the target.
        [Tooltip("Minimum and maximum range for the stopping range.")]
        public Vector2 StopAtTargetDist = new(18f, 22f);            // A minimum and maximum range for the stopping range.
        [HideInInspector] public float StopDistance;                // The distance between the tank and the target.
        [Range(0f, 2f)] public float TargetNavMeshUpdate = 0.2f;    // A delay between each targeting path update.
        [Header("Blending")]
        [Range(0f, 1f)] public float OrientSlerpScalar = 0.2f;      // A scalar for the slerp.
        // [Header("Target")]
        [HideInInspector] public Transform Target;                  // Reference to the target's transform.
        // [Header("NavMesh")]
        [HideInInspector] public float NavMeshUpdateDeadline;       // The time when the next path update is due.
        [Header("Firing")]
        [Tooltip("Minimum and maximum cooldown time delay between each firing in seconds.")]
        public Vector2 FireInterval = new(0.7f, 2.5f);              // A minimum and maximum cooldown time delay between each firing.
        [Tooltip("Force given to the shell if the fire button is not held, and the force given to the shell if the fire button is held for the max charge time in seconds.")]
        public Vector2 LaunchForceMinMax = new(6.5f, 30f);          // The force given to the shell if the fire button is not held, and the force given to the shell if the fire button is held for the max charge time.
        
        [Header("Evading")]
        [Range(0f, 300f)] public float maxFiringDistance = 35f;    // Maximum firing distance of player

        [Header("References")]
        [Tooltip("Prefab")] public Rigidbody Shell;                 // Prefab of the shell.
        [Tooltip("Transform")] public Transform FireTransform;      // A child of the tank where the shells are spawned.
        // public Slider AimSlider;                                 // A child of the tank that displays the current launch force.
        [Header("Firing Audio")]
        public AudioSource SFXAudioSource;                          // Reference to the audio source used to play the shooting audio. NB: different to the movement audio source.
        // public AudioClip ShotChargingAudioClip;                  // Audio that plays when each shot is charging up.
        public AudioClip ShotFiringAudioClip;                       // Audio that plays when each shot is fired.

        public float ActualFireInterval; // my initialization
        public float ShotCooldown; // the current cooldown
        public float DistanceToTarget; // self-explanatory...
        public bool isLowHealth = false; // indicator to move to HidingState
        public TankHealth health; // reference to TankHealth
        
        // aids for patrolling
        public Vector3 targetLastSeen = new Vector3(-1,-1,-1);
        public Vector3 defaultVector3 = new Vector3(-1,-1,-1);

        // for transition to RangeFinding State
        public float minDistToPlayer = 10f;


        // for target tracking
        [HideInInspector] public float TrackCooldown;
        public float ActualTrackInterval= 0.1f; //set interval in seconds to track current position of player tank
        public float Wigglefactor = 10f;// set multiplication factor for the slow player position update that will be used to check if they are wiggling to throw off our predictive aim
        private float WiggleTrackInterval; //interval between slow position updates
        private float WiggleCooldown;//current time left between slow position updates
        public float mstrShotLeadFactor = 1.05f; //multiplication factor to determine how far in front of the tanks direction to shoot
        [HideInInspector] public float shotLeadFactor; //current shot lead factor
        [HideInInspector] public Vector3 TargetStoredPos = new Vector3(-1,-1,-1);//stored location of player tank updated every ActualTrackInterval period
        [HideInInspector] public Vector3 NewSlowTargetStoredPos = new Vector3(-1,-1,-1);//slow stored location updated every WiggleTrackInterval period
        [HideInInspector] public Vector3 ActiveSlowTargetStoredPos = new Vector3(-1,-1,-1); //current active stored position updated to the new slow position every
        //WiggleTrackInterval so it lags the actual position of the player tank allowing for detection of if they are just wiggling back and forth to throw off predictive aiming
        [HideInInspector] private Vector3 barrelPosition;//offset position of barrel from center of tank
        private float barrelOffset;
        [HideInInspector] public float ShellVel;//shell velocity

        private float approxTargetTravel;
        private Vector3 tDir;

        // for evading
        float moveBackProb = 0.15f;
        float randomProb = 0.1f;
        Vector3 evadeDirection = new Vector3(0.0f, 0.0f, 0.0f);
        float evadeDistance = 5.0f;

        private bool m_Started = false; // Whether the tank has started moving.
        private Rigidbody m_Rigidbody;  // Reference used to the tank's regidbody.
        private TankSound m_TankSound;  // Reference used to play sound effects.


        /// <summary>
        /// Method <c>MoveTurnSound</c> returns the current tank's velocity.
        /// </summary>
        private Vector2 MoveTurnSound() => new Vector2(Mathf.Abs(NavMeshAgent.velocity.x), Mathf.Abs(NavMeshAgent.velocity.z));

        /// <summary>
        /// Method <c>GetInitialState</c> returns the initial state of the state machine.
        /// </summary>
        protected override BaseState GetInitialState() => m_States.GettingCenter;

        /// <summary>
        /// Method <c>SetNavMeshAgent</c> sets the NavMeshAgent's speed and angular speed.
        /// </summary>
        private void SetNavMeshAgent()
        {
            NavMeshAgent.speed = GameManager.Speed;
            NavMeshAgent.angularSpeed = GameManager.AngularSpeed;
        }

        /// <summary>
        /// Method <c>SetStopDistanceToZero</c> sets the NavMeshAgent's stopping distance to zero.
        /// </summary>
        public void SetStopDistanceToZero() => NavMeshAgent.stoppingDistance = 0f;

        /// <summary>
        /// Method <c>SetStopDistanceToTarget</c> sets the NavMeshAgent's stopping distance to the target's distance.
        /// </summary>
        public void SetStopDistanceToTarget() => NavMeshAgent.stoppingDistance = StopDistance;

        /// <summary>
        /// Method <c>Awake</c> is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            m_States = new States(this);

            GameManager = GameManager.Instance;

            m_Rigidbody = GetComponent<Rigidbody>();
            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_TankSound = GetComponent<TankSound>();

            SetNavMeshAgent();

            // TargetDistance = Random.Range(StartToTargetDist.x, StartToTargetDist.y);
            // StopDistance = Random.Range(StopAtTargetDist.x, StopAtTargetDist.y);
            
            // jon's initializations
            TargetDistance = 35f; // i set to max, tank better at long range, don't need space to hold
            StopDistance = 22f;
            ActualFireInterval = 0.7f;
            health = GetComponent<TankHealth>();

            //joe's initializations
            barrelPosition = new Vector3(FireTransform.position.x,transform.position.y,FireTransform.position.z);
            barrelOffset = Vector3.Magnitude(transform.position - FireTransform.position);
            //barrel offset vector from tank
            WiggleTrackInterval = ActualTrackInterval*Wigglefactor;
            WiggleCooldown = WiggleTrackInterval;
            shotLeadFactor = mstrShotLeadFactor;

            SetStopDistanceToTarget();
            var tankManagers = GameManager.PlayerPlatoon.Tanks.Take(1);
            if (tankManagers.Count() != 0)
                Target = tankManagers.First().Instance.transform;
            else
                Debug.LogError("'Player Platoon' is empty!");
        }

        /// <summary>
        /// Method <c>OnEnable</c> is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            // When the tank is turned on, make sure it's not kinematic.
            m_Rigidbody.isKinematic = false;
        }

        /// <summary>
        /// Method <c>Start</c> is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private new void Start()
        {
            // base.Start(); // Moved to Update.

            m_TankSound.MoveTurnInputCalc += MoveTurnSound;
        }

        /// <summary>
        /// Method <c>OnDisable</c> is called when the behaviour becomes disabled or inactive.
        /// </summary>
        private void OnDisable()
        {
            // When the tank is turned off, set it to kinematic so it stops moving.
            m_Rigidbody.isKinematic = true;

            m_TankSound.MoveTurnInputCalc -= MoveTurnSound;
        }

        /// <summary>
        /// Method <c>Update</c> is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private new void Update()
        {
            if (!m_Started && GameManager.IsRoundPlaying)
            {
                m_Started = true;
                base.Start();
            }
            else if (GameManager.IsRoundPlaying)
            {
                base.Update();
            }
            else
            {
                m_Started = false;
                StopAllCoroutines();
            }
        }
        private new void LateUpdate()
        {
            TrackCooldown -= Time.deltaTime;// countdown time from last track update
            WiggleCooldown -= Time.deltaTime; //countdown time from last slow track update
            if(TrackCooldown > 0) return; // if time elapsed from last update not greater than track interval do nothing
            TrackCooldown = ActualTrackInterval;//reset cooldown
            TargetStoredPos = Target.position; //update track position
         
            if(WiggleCooldown > 0) return;//if time elapsed from last slow update not greater than track interval do nothing
            WiggleCooldown = WiggleTrackInterval;//reset slow cooldown

            ActiveSlowTargetStoredPos = NewSlowTargetStoredPos;//update active slow position to the new position from previous update
            NewSlowTargetStoredPos = Target.position;//update new slow position to player tank current location
        }

        /// <summary>
        /// Method <c>LaunchProjectile</c> instantiate and launch the shell.
        /// </summary>
        public void LaunchProjectile(float launchForce = 1f)
        {
            // makes sure that I respect the cooldown
            ShotCooldown -= Time.deltaTime;
            if(ShotCooldown > 0) return;
            ShotCooldown = ActualFireInterval;

            // Create an instance of the shell and store a reference to it's rigidbody.
            Rigidbody shellInstance = Instantiate(Shell, FireTransform.position, FireTransform.rotation) as Rigidbody;

            // Set the shell's velocity to the launch force in the fire position's forward direction.
            shellInstance.velocity = launchForce * FireTransform.forward; ;

            // Change the clip to the firing clip and play it.
            SFXAudioSource.clip = ShotFiringAudioClip;
            SFXAudioSource.Play();
        }


        // function to be called every update to transition to Hiding State
        public void CheckHealth(){
            if(isLowHealth) return;
            
            // based on max damage in ShellExplosion -> find a way to grab this dynamically
            if(health.m_CurrentHealth < 12.5f)
                isLowHealth = true;
        }


        public void UpdateDistanceToTarget(){
            if(Target == null) return;
            DistanceToTarget = Vector3.Distance(this.transform.position, Target.position);
        }

        // implements target prediction
        public float GetShellVelocity(){
            if(Target == null) return 0.0f; //if no target do nothing
            
            float wiggleTrackProb = 0.6f;
            float gravity = 9.81f;

            //if the distance of the tank from the lagged slow position is less than 0.6 of the expected value if the tank were going at full speed in a straight line
            if(Vector3.Magnitude(Target.position - ActiveSlowTargetStoredPos) < GameManager.Speed * WiggleTrackInterval * wiggleTrackProb)
            {
                shotLeadFactor = 0.0f; //set shot lead factor low to hit the player tank even if wiggling
            }
            else
            {
                shotLeadFactor = mstrShotLeadFactor; //set shot lead factor to the master value
            }

            tDir = Vector3.Normalize(Target.position - TargetStoredPos); //unit vector for tank direction
            

            float precisionDistToTarget = Vector3.Distance(this.transform.position+transform.forward*barrelOffset, Target.position + tDir * GameManager.Speed * shotLeadFactor);
            float flightTime = Mathf.Sqrt(2*((1.7f + DistanceToTarget*Mathf.Tan(10*Mathf.PI/180))/gravity));//approximate flight time of shell based on current dist to player tank

            approxTargetTravel = GameManager.Speed * flightTime;// travel of player tank in the approx flight time of the shell

            float shellVelocity = precisionDistToTarget/(flightTime*Mathf.Cos(10*Mathf.PI/180));//calculate shell velocity based on precise distance

            if (shellVelocity < LaunchForceMinMax.x) //if below min shell velocity set to min
            {
                shellVelocity = LaunchForceMinMax.x;
            }
            else if (shellVelocity > LaunchForceMinMax.y) //if above max shell velocity set to max
            {
                shellVelocity = LaunchForceMinMax.y;
            }

            return shellVelocity;

        }    

        public void Aim()
        {
            // old:
            // var lookPos = Target.position + approxTargetTravel*tDir*shotLeadFactor - this.transform.position;//set look position to the predicted point in front of player
            // lookPos.y = 0f;
            // var rot = Quaternion.LookRotation(lookPos); //turn to face look position
            // this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, this.OrientSlerpScalar);//turn at maximum turn rate


            // Calculate the desired rotation based on the target position.
            var lookPos = Target.position + approxTargetTravel * tDir * shotLeadFactor - this.transform.position;
            lookPos.y = 0f;
            var desiredRotation = Quaternion.LookRotation(lookPos);

            // Calculate the rotation step based on the AngularSpeed.
            float rotationStep = GameManager.AngularSpeed * Time.deltaTime;

            // Rotate towards the desired rotation with a limited speed.
            this.transform.rotation = Quaternion.RotateTowards(this.transform.rotation, desiredRotation, rotationStep);
        }


        public void AttackTarget(){
            float shellVelocity = GetShellVelocity();
            LaunchProjectile(shellVelocity);
        }
        
        public bool IsUnderAttack(){
            // Vector in direction of player
            Vector3 direction = Target.transform.forward;
            
            // Draw ray from position of player
            Ray ray = new Ray(Target.transform.position, direction);
            LayerMask mask = LayerMask.GetMask("AI");

            RaycastHit hitInfo;

            return Physics.Raycast(ray, out hitInfo, maxFiringDistance, mask);
        }


        public void AvoidAlly(Vector3 allyPosition){
            // rotate around target, maintaining distance
            Vector3 toTarget = Target.position - transform.position; 
            toTarget = toTarget.normalized;
            evadeDirection = Quaternion.AngleAxis(90.0f, Vector3.up) * toTarget;
            Vector3 m_Destination = Target.transform.position + evadeDirection*TargetDistance;
            NavMeshAgent.SetDestination(m_Destination);
        }

        // For JUSTIN:
        // TODO: If tank under attack, how to shift 
        public void AvoidEnemy(){
            float prob = Random.Range(0.0f, 1.0f);
            evadeDistance = Random.Range(3.0f, 6.0f);

            // you can grab the enemy target position and rotation using the `Target` Variable.
            Vector3 toTarget = Target.position - transform.position; 
            toTarget = toTarget.normalized;

            // // 15% chance to move straight backwards
            if (prob < moveBackProb) {
                evadeDirection = -1 * toTarget;
            }
            

            // 10% chance for totally random movement
            if (prob >= (1.0f - randomProb)){
                evadeDirection = Quaternion.AngleAxis(Random.Range(0.0f, 360.0f), Vector3.up) * toTarget;
            }

            // Remainder for moving perpendicularly
            if (prob >= moveBackProb && prob < (1.0f - randomProb)){
                int halfProb = Random.Range(0, 2);
                if (halfProb == 0){
                    evadeDirection = Quaternion.AngleAxis(90.0f, Vector3.up) * toTarget;
                }

                if (halfProb == 1){
                    evadeDirection = Quaternion.AngleAxis(-90.0f, Vector3.up) * toTarget;
                }
            }

            // destination for moving in the other direction 
            Vector3 m_Destination = transform.position + evadeDirection*evadeDistance;
            NavMeshAgent.SetDestination(m_Destination);
        }


        public Vector3 GetObstructionCoords(){
            bool blocked = false;
            Vector3 shell_destination = Target.position + new Vector3(0, FireTransform.position.y / 2, 0); // assign ray to hit middle of tank (if hit top, can't detect low obstacles; bottom - may sense ground / v low dunes)
            // layer mask to only AI and default 
            int layermask_AI = 1 << 11; // 11 represents AI Tank layer 
            int layermask_default = 1 << 0; // 0 represents default layer where the background objects are 
            int layermask = layermask_AI | layermask_default;
            
            blocked = Physics.Linecast(FireTransform.position, shell_destination, out RaycastHit hitInfo, layermask);
            Debug.DrawLine(FireTransform.position, shell_destination, blocked ? Color.red : Color.green);
            if (blocked)
            {
                Debug.Log("Hit: " + hitInfo.transform.name + ". Collider: " + hitInfo.collider + ". By tank: " + this.transform.position);
                return hitInfo.transform.position;
            }
            return Vector3.zero;
        }

        // bundles top functions together
        public void HyperAggression(){
            Debug.Log("Target at: " + Target.transform.position);
            CheckHealth();
            UpdateDistanceToTarget();
            if(DistanceToTarget > TargetDistance) return;
            
            Aim();
            Vector3 obstacle = GetObstructionCoords();
            if(obstacle == Vector3.zero){
                AttackTarget();
            }else {
                AvoidAlly(obstacle);
            }
        }
    }
}
