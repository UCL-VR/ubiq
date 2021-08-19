using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PuzzleMaker
{

    public struct SJointInfo
    {
        private EJointType _jointType;
        private EJointPosition _jointPosition;
        private int _jointWidth;
        private int _jointHeight;

        public EJointType JointType
        {
            get { return _jointType; }
        }

        public EJointPosition JointPosition
        {
            get { return _jointPosition; }
        }

        public int JointWidth
        {
            get { return _jointWidth; }
        }

        public int JointHeight
        {
            get { return _jointHeight; }
        }


        public SJointInfo(EJointType _JointType, EJointPosition _JointPosition, int _JointWidth, int _JointHeight)
        {
            _jointType = _JointType;
            _jointPosition = _JointPosition;

            _jointWidth = _JointWidth;
            _jointHeight = _JointHeight;
        }


        public override string ToString()
        {
            string Result = "JointType : " + _jointType.ToString();
            Result = Result + "\r\n JointPosition : " + _jointPosition.ToString();
            Result = Result + "\r\n JointWidth : " + _jointWidth.ToString();
            Result = Result + "\r\n JointHeight : " + _jointHeight.ToString();
            Result = Result + "\r\n";

            return Result;
        }

    }


    public enum EJointType
    {
        Male = 0,
        Female = 1
    }


    public enum EJointPosition
    {
        Top = 0,
        Left = 1,
        Right = 2,
        Bottom = 3
    }

}
