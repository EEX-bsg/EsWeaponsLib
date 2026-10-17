# Block Physics (EwlBlockPhysics)

`EwlBlockPhysics` is an XML custom module that configures the physics of the block itself rather than a weapon.
Every element can be omitted, and anything you do not write stays at Besiege's value. All elements are listed in the [XML Reference](../reference/xml-modules.md#ewlblockphysics).

```xml
<Modules>
    <EwlBlockPhysics modid="0d219960-b202-4906-b29d-8d90a61a89b8">
        <JointStrength>INF</JointStrength>
        <Material>
            <Friction>0.8</Friction>
            <Bounciness>0</Bounciness>
        </Material>
    </EwlBlockPhysics>
</Modules>
```

## Joint strength

`JointStrength` is the force and torque at which the joint with the parent block breaks. `INF` makes it unbreakable. If you do not write it, nothing changes (0 makes a joint that breaks under any force at all).

## Physics material

Writing `Material` sets friction and bounciness on all of the block's colliders (including `AdditionalColliders`).

## Joint rotation axes

Writing `Rotation` makes the block able to rotate relative to its parent block (swaying flags and antennas, for example).

```xml
<Rotation>
    <AxisX>Limited</AxisX>   <!-- Locked (default) / Limited / Free -->
    <AxisY>Locked</AxisY>
    <AxisZ>Locked</AxisZ>
    <Limit>30</Limit>        <!-- Range of motion of a Limited axis [deg] -->
    <Spring>50</Spring>      <!-- Strength of the return to the original orientation -->
    <Damper>5</Damper>
</Rotation>
```

## Adding colliders

`AdditionalColliders` adds colliders to the block, using the same format as the block's `Colliders`. `BoxCollider` can also have a `Center`.

```xml
<AdditionalColliders>
    <BoxCollider layer="12">
        <Position x="0" y="0.5" z="0" />
        <Rotation x="0" y="0" z="0" />
        <Scale x="1" y="0.1" z="1" />
    </BoxCollider>
</AdditionalColliders>
```

- With `layer="12"`, other blocks can be placed there, and at simulation start that block connects to this block (for example, a platform to mount blocks on).
- A `BoxCollider` can be placed on any of the six faces, at the position of the clicked point rounded to a one-cell unit from the face center.
- `SphereCollider` / `CapsuleCollider` are placed at "the collider's position + the direction of the face that was hit × 0.5", the same as in standard Besiege.
- Changing `AdditionalColliders` requires a Besiege restart.
