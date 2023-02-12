using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using System;

using static System.MathF;
using static Unity.Mathematics.math;

public class SimpleGravity : GfMovementSimple
{
    [SerializeField]
    protected float m_physUpdateInterval = 0.05f;

    protected float m_timeUntilPhys = 0;
    protected override void BeforePhysChecks(float deltaTime)
    {
        m_touchedParent = false;

        CalculateEffectiveValues();
        CalculateVelocity(deltaTime, Zero3);
    }

    protected override void CalculateVelocity(float deltaTime, Vector3 movDir)
    {
        Vector3 slope = m_slopeNormal;
        float movDirMagnitude = movDir.magnitude;
        if (movDirMagnitude > 0.000001f) GfTools.Div3(ref movDir, movDirMagnitude); //normalise

        float verticalFallSpeed = Vector3.Dot(slope, m_velocity);
        float fallMagn = 0, fallMaxDiff = -verticalFallSpeed - m_maxFallSpeed; //todo
        //remove vertical factor from the velocity to calculate the horizontal plane velocity easier
        Vector3 effectiveVelocity = m_velocity;

        if (!CanFly)
        {
            //remove vertical component of velocity if we can't fly
            effectiveVelocity.x -= slope.x * verticalFallSpeed;
            effectiveVelocity.y -= slope.y * verticalFallSpeed;
            effectiveVelocity.z -= slope.z * verticalFallSpeed;

            if (fallMaxDiff < 0)
                fallMagn = Min(-fallMaxDiff, m_mass * deltaTime); //speed under maxFallSpeed         
            else
                fallMagn = -Min(fallMaxDiff, m_effectiveDeacceleration * deltaTime);//speed equal to maxFallSpeed or higher
        }

        float currentSpeed = effectiveVelocity.magnitude;
        float dotMovementVelDir = 0;
        if (currentSpeed > 0.000001F)
        {
            Vector3 velDir = effectiveVelocity;
            GfTools.Div3(ref velDir, currentSpeed);
            dotMovementVelDir = Vector3.Dot(movDir, velDir);
        }

        float desiredSpeed = m_speed * movDirMagnitude;
        float speedInDesiredDir = currentSpeed * Max(0, dotMovementVelDir);

        float minAux = Min(speedInDesiredDir, desiredSpeed);
        Vector3 unwantedVelocity = effectiveVelocity;
        unwantedVelocity.x -= movDir.x * minAux;
        unwantedVelocity.y -= movDir.y * minAux;
        unwantedVelocity.z -= movDir.z * minAux;

        float unwantedSpeed = unwantedVelocity.magnitude;
        if (unwantedSpeed > 0.000001F) GfTools.Div3(ref unwantedVelocity, unwantedSpeed);
        float deaccMagn = Min(unwantedSpeed, m_effectiveDeacceleration * deltaTime);

        GfTools.Mult3(ref unwantedVelocity, deaccMagn);
        GfTools.Mult3(ref slope, fallMagn);

        GfTools.Minus3(ref m_velocity, unwantedVelocity);//add deacceleration
        GfTools.Minus3(ref m_velocity, slope); //add vertical speed change
    }

    void Update()
    {
        float delta = Time.deltaTime;
        Move(delta);

        m_timeUntilPhys -= delta;
        if (m_timeUntilPhys <= 0)
        {
            UpdatePhysics(m_physUpdateInterval - m_timeUntilPhys, false);
            m_timeUntilPhys += m_physUpdateInterval;
        }
    }
}
