using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace PuzzleMaker
{

    public class SPieceInfo
    {
        private int _ID = 0;
        private List<SJointInfo> _Joints = new List<SJointInfo>();

        public int ID { get { return _ID; } }

        public int TotalJoints
        {
            get { return _Joints.Count; }
        }

        public SJointInfo this[EJointPosition i]
        {

            get
            {
                foreach (SJointInfo item in _Joints)
                {
                    if (item.JointPosition == i)
                        return item;
                }

                throw new System.ArgumentException("Joint with provided JointPosition not found");
            }

        }

        public bool AddJoint(SJointInfo Joint)
        {
            //Check if this joint type already exists
            foreach (SJointInfo item in _Joints)
                if (item.JointPosition == Joint.JointPosition)
                    return false;

            _Joints.Add(Joint);

            return true;
        }

        public SPieceInfo(int PieceID)
        {
            _ID = PieceID;
        }

        public bool HaveJoint(EJointPosition Joint)
        {
            for (int i = 0; i < _Joints.Count; i++)
            {
                if (_Joints[i].JointPosition == Joint)
                    return true;
            }

            return false;
        }


        public SJointInfo[] GetJoints()
        {
            SJointInfo[] Result = new SJointInfo[_Joints.Count];
            _Joints.CopyTo(Result);
            return Result;
        }


        public SJointInfo GetJoint(EJointPosition Joint, out bool IsFound)
        {
            SJointInfo Result = new SJointInfo();

            foreach (SJointInfo item in _Joints)
            {
                if (item.JointPosition == Joint)
                {
                    Result = new SJointInfo(item.JointType, item.JointPosition, item.JointWidth, item.JointHeight);
                    IsFound = true;

                    return Result;
                }
            }

            IsFound = false;

            return Result;
        }


        public SPieceInfo MakeCopy()
        {
            SPieceInfo Temp = new SPieceInfo(_ID);
            foreach (SJointInfo item in _Joints)
                Temp.AddJoint(item);

            return Temp;
        }


        public override string ToString()
        {

            string Result = "";
            Result = "Piece ID : " + _ID.ToString() + "\r\n";
            foreach (SJointInfo item in _Joints)
                Result = Result + item.ToString() + "\r\n";


            return Result;
        }

    }

}
