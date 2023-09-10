# 23s1-ce6127-tanks-ai-2022-3-4f1

- [ ] Refactor IdleState
- [ ] Refactor PatrollingState
- [ X ] Set up ChasingState class barebones
- [ X ] Set up HidingState class barebones
- [ X ] Set up RepositioningState class barebones
- [ ] Set up function to move to target
- [ ] Set up function to calculate offset for target firing
- [ ] From any state: Check if tank is lowhealth -> transition to HidingState
- [ ] From Repositioning: Check if target is within range -> transition to ChasingState
- [ ] From Chasing: If target not within rage -> transition to Repositioning State
- [ ] Set up function to detect if obstacle in front (i.e. will take damage)
- [ ] Stop shooting if obstacle distance is such that AI tank will take damage
- [ ] Set up function to detect if friendly tank in front
- [ ] Find Good Waypoints to hide and orient so that player cannot attack
