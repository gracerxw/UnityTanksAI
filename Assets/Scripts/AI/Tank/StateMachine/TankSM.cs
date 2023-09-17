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
            public ChasingState Chasing;
            public RepositioningState Repositioning;
            public RangeFindingState RangeFinding;
            public EvadingState Evading;

            internal States(TankSM sm)
            {
                Patrolling = new PatrollingState(sm);
                Hiding = new HidingState(sm);
                Chasing = new ChasingState(sm);
                Repositioning = new RepositioningState(sm);
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
        public float ActualTrackInterval=0.1f; //set interval in seconds to track current position of player tank
        public float Wigglefactor = 10f;// set multiplication factor for the slow player position update that will be used to check if they are wiggling to throw off our predictive aim
        private float WiggleTrackInterval; //interval between slow position updates
        private float WiggleCooldown;//current time left between slow position updates
        public float mstrShotLeadFactor = 1.05f; //multiplication factor to determine how far in front of the tanks direction to shoot
        [HideInInspector] public float shotLeadFactor; //current shot lead factor
        [HideInInspector] public Vector3 TargetStoredPos =new Vector3(-1,-1,-1);//stored location of player tank updated every ActualTrackInterval period
        [HideInInspector] public Vector3 NewSlowTargetStoredPos =new Vector3(-1,-1,-1);//slow stored location updated every WiggleTrackInterval period
        [HideInInspector] public Vector3 ActiveSlowTargetStoredPos =new Vector3(-1,-1,-1); //current active stored position updated to the new slow position every
        //WiggleTrackInterval so it lags the actual position of the player tank allowing for detection of if they are just wiggling back and forth to throw off predictive aiming
        [HideInInspector] private Vector3 barrelPosition;//offset position of barrel from center of tank
        private float barrelOffset;
        [HideInInspector] public float ShellVel;//shell velocity

          private float approxtargetTravel;

        private Vector3 tDir;

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
        protected override BaseState GetInitialState() => m_States.Patrolling;

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
            barrelPosition= new Vector3(FireTransform.position.x,transform.position.y,FireTransform.position.z);
            barrelOffset = Vector3.Magnitude(transform.position - FireTransform.position);
            //barrel offset vector from tank
            WiggleTrackInterval=ActualTrackInterval*Wigglefactor;
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
            if(TrackCooldown > 0) return; //if time elapsed from last update not greater than track interval do nothing
            TrackCooldown = ActualTrackInterval;//reset cooldown
            TargetStoredPos = Target.position; //update track position
         
           
            if(WiggleCooldown > 0) return;//if time elapsed from last slow update not greater than track interval do nothing
            WiggleCooldown = WiggleTrackInterval;//reset slow cooldown
            //Debug.Log("dist" + Vector3.Magnitude(Target.position - ActiveSlowTargetStoredPos));
            //Debug.Log("control" + GameManager.Speed*WiggleTrackInterval);

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

        // reorient to face the target
      /*  public void FaceTarget(){
            var lookPos = Target.position - this.transform.position;
            lookPos.y = 0f;
            var rot = Quaternion.LookRotation(lookPos);
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, this.OrientSlerpScalar);
        }
*/
        public void UpdateDistanceToTarget(){
            if(Target == null) return;
            DistanceToTarget = Vector3.Distance(this.transform.position, Target.position);
        }


        // FOR JOE
        // TODO: 
        // 1. launch target within constraints
        // 1.5 If not within constraints just return
        // 2. Get appropriate angle + rotation
        // 3. Calculate appropriate force based on relative velocity + position

        public void TargetPrediction(){
            if(Target == null) return; //if no target do nothing

            if(Vector3.Magnitude(Target.position - ActiveSlowTargetStoredPos) < GameManager.Speed*WiggleTrackInterval*0.6f)//if the distance of the tank from the lagged slow
            //position is less than 0.6 of the expected value if the tank were going at full speed in a straight line
            {
                shotLeadFactor = mstrShotLeadFactor*0.0f;//set shot lead factor low to hit the player tank even if wiggling
                //Debug.Log(Vector3.Magnitude(Target.position - ActiveSlowTargetStoredPos) + "wiggle detected - adjusting range");
            }
            else
            {
                shotLeadFactor = mstrShotLeadFactor;//set shot lead factor to the master value
                //Debug.Log(Vector3.Magnitude(Target.position - ActiveSlowTargetStoredPos) + "Normal engagement speed");
            }
            tDir = Vector3.Normalize(Target.position - TargetStoredPos); //unit vector for tank direction
            
            //Debug.DrawRay(Target.position, tDir*(GameManager.Speed*Time.deltaTime),Color.green,3);
            float approxFlightTime =Mathf.Sqrt(2*((1.7f + DistanceToTarget*Mathf.Tan(10*Mathf.PI/180))/9.81f));//approximate flight time of shell based on current dist to player tank
            Debug.Log("approx" + approxFlightTime);
            approxtargetTravel = GameManager.Speed*approxFlightTime;// travel of player tank in the approx flight time of the shell

            float PrecisionDistToTarget = Vector3.Distance(this.transform.position+transform.forward*barrelOffset, Target.position + tDir*approxtargetTravel*shotLeadFactor); 
            //recalculate a more accurate dist from barrel to predicted location
            float FlightTime = Mathf.Sqrt(2*((1.7f + PrecisionDistToTarget*Mathf.Tan(10*Mathf.PI/180))/9.81f));//recalculate flight time with accurate distance
            ShellVel = PrecisionDistToTarget/(FlightTime*Mathf.Cos(10*Mathf.PI/180));//calculate shell velocity based on precise distance
            Debug.Log("prec" + FlightTime);
            if (ShellVel < LaunchForceMinMax.x) //if below min shell velocity set to min
            {
                ShellVel = LaunchForceMinMax.x;
            }
            else if (ShellVel > LaunchForceMinMax.y) //if above max shell velocity set to max
            {
                ShellVel = LaunchForceMinMax.y;
            }

            //var lookPos = Target.position + approxtargetTravel*tDir*shotLeadFactor - this.transform.position;//set look position to the predicted point in front of player
            //lookPos.y = 0f;
            //var rot = Quaternion.LookRotation(lookPos); //turn to face look position
            //this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, this.OrientSlerpScalar);//turn at maximum turn rate
            //Debug.DrawRay(Target.position, tDir*approxtargetTravel,Color.red,3);
            //Debug.DrawRay(Target.position,Target.transform.forward*10,Color.green,3);
            //Debug.Log(ShellVel);
        }    
        public void Aim()
        {
            var lookPos = Target.position + approxtargetTravel*tDir*shotLeadFactor - this.transform.position;//set look position to the predicted point in front of player
            lookPos.y = 0f;
            var rot = Quaternion.LookRotation(lookPos); //turn to face look position
            this.transform.rotation = Quaternion.Slerp(this.transform.rotation, rot, this.OrientSlerpScalar);
        }
        public void AttackTarget(float offset = 0f){
            // offset because the tanks will be in motion, 
            // to refine: can calculate whether the target is moving away + whether you are moving closer
           // if(offset == 0f){
             //   offset = Random.Range(-3f, 3f);
            //}
            LaunchProjectile(ShellVel);
        }


        // For JUSTIN
        // TODO: Find if player tank is oriented to AI tank
        // 1. Calculate rotation of player tank
        // 2. Calculate the Vector3 between players
        // 3. Check if within range + if the player tank is at the correct angle to attack
        public bool IsUnderAttack(){
            return false;
        }


        // For JUSTIN:
        // TODO: If ally nearby, how to shift 
        public void AvoidAlly(Vector3 allyPosition){
            return;
        }

        // For JUSTIN:
        // TODO: If tank under attack, how to shift 
        public void AvoidEnemy(){
            // you can grab the enemy target position and rotation using the `Target` Variable.
            return;
        }


        // For JON (for Repositioning Class)
        // TODO: check if an ally is within radius and skeet away
        public bool IsAllyInRadius(){
            return false;
        }


        // For GRACE: 
        // TODO: check if there is an environment obstacle / ally tank that will block the shot to enemy
        public bool IsObstructionPresent(){
            return false;
        }



        // bundles top functions together
        public void HyperAggression(){
            CheckHealth();
            UpdateDistanceToTarget();
            if(DistanceToTarget > TargetDistance) return;
            //FaceTarget();
            
            TargetPrediction();
            Aim();
            if(IsObstructionPresent()) return;
            AttackTarget();
        }
    }
}
