using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;
using BulletXNA.BulletCollision;
using System.Security.Cryptography;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //Settings

        string chairNamePitch = "Motor Chair Pitch";
        string chairNameYaw = "Motor Chair Yaw";
        string chairNameRoll = "Motor Chair Roll";

        string gyroName = "Gyroskop Ship";
        string remoteShipName = "Remote Ship";
        string remoteChairName = "Remote Chair";
        string debugLCDName = "Debug";

        const string toggleAutoTargetArgument = "Auto";
        const string manualSelectTargetArgument = "SetTarget";

        int chairLockForce = 10;
        int chairMotorPrecision = 2; // Nachkommastellen
        int shipGyroPrecision = 2; // Nachkommastellen

        bool useRoll = true;

        //Settings End

        bool shipRotating = false;
        bool chairRotating = false;

        bool autoSetTarget = false;


        IMyMotorStator[] chairMotors = new IMyMotorStator[3];
        IMyGyro gyro;
        IMyRemoteControl remoteChair, remoteShip;
        IMyTextPanel lcd;

        MatrixD targetMatrix;
        Vector3 lastShipAngles;
        Vector3 lastChairAngles;

        public Program()
        {
            chairMotors[0] = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(chairNamePitch);
            chairMotors[1] = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(chairNameYaw);
            chairMotors[2] = (IMyMotorStator)GridTerminalSystem.GetBlockWithName(chairNameRoll);

            remoteChair = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName(remoteChairName);
            remoteShip = (IMyRemoteControl)GridTerminalSystem.GetBlockWithName(remoteShipName);

            lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(debugLCDName);

            gyro = (IMyGyro)GridTerminalSystem.GetBlockWithName(gyroName);

            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            Initialize();
        }
        
        public void Save()
        {
            
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // Variables
            MatrixD ChairMatrix = GetBlock2WorldTransform(remoteChair);
            MatrixD shipMatrix = GetBlock2WorldTransform(remoteShip);

            Vector3 shipAngles = getRotationAngles(targetMatrix, shipMatrix);
            Vector3 chairAngles = getRotationAngles(targetMatrix, ChairMatrix);

            // Angular Speed
            Vector3 angularSpeedShip = (lastShipAngles - shipAngles) * 50;
            lastShipAngles = shipAngles;

            Vector3 angularSpeedChair = (lastChairAngles - chairAngles) * 20;
            lastChairAngles = chairAngles;

            bool shipReachedTarget = shipAngles.Length() < 0.01 && angularSpeedShip.Length() < 0.01F;
            bool chairReachedTarget = chairAngles.Length() < 0.1F && angularSpeedChair.Length() < 0.01F;

            // Rotating
            if (chairRotating)
            {
                UpdateChairRotation(chairAngles - angularSpeedChair);

                if (chairReachedTarget && shipReachedTarget)
                {
                    ChairReachedDestination();
                }
            }

            if (shipRotating)
            {
                if (shipReachedTarget)
                {
                    ShipReachedDestination();
                }
            }

            // Rotation Updates
            UpdateGyroRotation(shipAngles - angularSpeedShip);
            if (autoSetTarget)
            {
                if (chairAngles.Length() > 0.2F && angularSpeedChair.Length() <= 0.0001F)
                {
                    SetDestionation(ChairMatrix);
                }
            }

            // Argument Handling
            switch (argument) 
            {
                case manualSelectTargetArgument:
                    SetDestionation(ChairMatrix);
                    break;
                case toggleAutoTargetArgument:
                    ToggleAutoSetTargte();
                    break;
            }

            lcd.WriteText((chairRotating ? "ChairRotating" : "") +"\n"+ (shipRotating ? "shipRotating" : "") + "\n" + (autoSetTarget ? "autoSetTarget" : ""));
        }

        private void Initialize()
        {
            SetDestionation(GetBlock2WorldTransform(remoteShip));
            setChairRotating(true);
        }

        private void ToggleAutoSetTargte()
        {
            autoSetTarget = !autoSetTarget;
        }

        private void ChairReachedDestination()
        {
            setChairRotating(false);
        }
        private void ShipReachedDestination()
        {
            SetShipRotating(false);
        }

        private void SetDestionation(MatrixD ChairMatrix)
        {
            targetMatrix = ChairMatrix;
            SetShipRotating(true);
            setChairRotating(true);
        }

        private void SetShipRotating(bool on)
        {
            shipRotating = on;
        }

        private void setChairRotating(bool on)
        {
            chairRotating = on;

            chairMotors[0].Enabled = on;
            chairMotors[1].Enabled = on;
            chairMotors[2].Enabled = on;
        }

        private void UpdateChairRotation(Vector3 saveAngles)
        {
            chairMotors[0].TargetVelocityRPM = (float)Math.Round(saveAngles.X * chairLockForce, chairMotorPrecision);
            chairMotors[1].TargetVelocityRPM = (float)Math.Round(saveAngles.Y * -chairLockForce, chairMotorPrecision);
            chairMotors[2].TargetVelocityRPM = useRoll ? (float)Math.Round(saveAngles.Z * chairLockForce, chairMotorPrecision) : 0;
        }

        private void UpdateGyroRotation(Vector3 gyroTargets)
        {
            gyro.Pitch = (float)Math.Round(gyroTargets.X, shipGyroPrecision);
            gyro.Yaw = (float)Math.Round(gyroTargets.Y, shipGyroPrecision);
            gyro.Roll = useRoll ? (float)Math.Round(gyroTargets.Z, shipGyroPrecision) : 0;
        }

        Vector3 getRotationAngles(MatrixD ChairMatrix, MatrixD shipMatrix) {

            Vector3D chairForward = Vector3D.Normalize(ChairMatrix.Forward);
            Vector3D chairRight = Vector3D.Normalize(ChairMatrix.Right);

            Vector3D shipUp = Vector3D.Normalize(shipMatrix.Up);
            Vector3D shipRight = Vector3D.Normalize(shipMatrix.Right);

            Vector3 angles;

            angles.X = (float)Math.Asin(Vector3D.Dot(shipUp, chairForward));
            angles.Y = (float)Math.Asin(Vector3D.Dot(shipRight, chairForward));
            angles.Z = (float)Math.Asin(Vector3D.Dot(shipUp, chairRight));

            return angles;
        }

        MatrixD GetBlock2WorldTransform(IMyCubeBlock blk)
        {
            Matrix blk2grid;
            blk.Orientation.GetMatrix(out blk2grid);
            return blk2grid *
            MatrixD.CreateTranslation(((Vector3D)new Vector3D(blk.Min + blk.Max)) / 2.0) *
            GetGrid2WorldTransform(blk.CubeGrid);
        }
        MatrixD GetGrid2WorldTransform(IMyCubeGrid grid)
        {
            Vector3D origin = grid.GridIntegerToWorld(new Vector3I(0, 0, 0));
            Vector3D plusY = grid.GridIntegerToWorld(new Vector3I(0, 1, 0)) - origin;
            Vector3D minusZ = grid.GridIntegerToWorld(new Vector3I(0, 0, -1)) - origin;
            return MatrixD.Rescale(MatrixD.CreateWorld(origin, minusZ, plusY), grid.GridSize);
        }

        void PrintVector(Vector3 vec)
        {
            lcd.WriteText(Math.Round(vec.X, 4) + "\n" + Math.Round(vec.Y, 4) + "\n" + Math.Round(vec.Z, 4) + "\n" + "\n", true);
        }
    }
}
