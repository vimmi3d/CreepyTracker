using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace Microsoft.Samples.Kinect.BodyBasics
{
    internal class Skeleton
    {
        private List<JointType> BodyConfidenceAcceptedJoints = new List<JointType>()
        {
            JointType.Head,

            JointType.ShoulderLeft,
            JointType.ElbowLeft,
            JointType.HandLeft,
            JointType.HipLeft,
            JointType.KneeLeft,
            JointType.AnkleLeft,

            JointType.ShoulderRight,
            JointType.ElbowRight,
            JointType.HandRight,
            JointType.HipRight,
            JointType.KneeRight,
            JointType.AnkleRight,

            JointType.SpineMid,
            JointType.SpineShoulder
        };

        private Dictionary<string, int> JointsConfidenceWeight;

        public Skeleton(Body body, Dictionary<string, int> jointsConfidenceWeight)
        {
            this.JointsConfidenceWeight = jointsConfidenceWeight;

            // get the coordinate mapper
            CoordinateMapper coordinateMapper = KinectSensor.GetDefault().CoordinateMapper;

            Message = ""  
            + BodyPropertiesTypes.UID.ToString() + MessageSeparators.SET + body.TrackingId
            + MessageSeparators.L2 + BodyPropertiesTypes.Confidence.ToString() + MessageSeparators.SET + BodyConfidence(body)
            + MessageSeparators.L2 + BodyPropertiesTypes.HandLeftState.ToString() + MessageSeparators.SET + body.HandLeftState
            + MessageSeparators.L2 + BodyPropertiesTypes.HandLeftConfidence.ToString() + MessageSeparators.SET + body.HandLeftConfidence
            + MessageSeparators.L2 + BodyPropertiesTypes.HandRightState.ToString() + MessageSeparators.SET + body.HandRightState
            + MessageSeparators.L2 + BodyPropertiesTypes.HandRightConfidence.ToString() + MessageSeparators.SET + body.HandRightConfidence
            + MessageSeparators.L2 + HandScreenSpace.HandLeftPosition.ToString() + MessageSeparators.SET + convertCameraDepthPointToStringRPC(coordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.HandLeft].Position))
            + MessageSeparators.L2 + HandScreenSpace.HandRightPosition.ToString() + MessageSeparators.SET + convertCameraDepthPointToStringRPC(coordinateMapper.MapCameraPointToDepthSpace(body.Joints[JointType.HandRight].Position));
            
            foreach (JointType j in Enum.GetValues(typeof(JointType)))
            {
                Message += "" + MessageSeparators.L2 + j.ToString() + MessageSeparators.SET + convertVectorToStringRPC(body.Joints[j].Position);
            }
        }

        public string Message { get; internal set; }   

        private int BodyConfidence(Body body)
        {
            int confidence = 0;

            foreach (Joint j in body.Joints.Values)
            {
                if (BodyConfidenceAcceptedJoints.Contains(j.JointType) && j.TrackingState == Microsoft.Kinect.TrackingState.Tracked)
                {
                    //    if (j.JointType == Microsoft.Kinect.JointType.HandLeft || j.JointType == Microsoft.Kinect.JointType.HandRight)
                    //        confidence += 3;
                    //    else
                    //        confidence += 1;

                    if (JointsConfidenceWeight.ContainsKey(j.JointType.ToString()))
                    {
                        confidence += JointsConfidenceWeight[j.JointType.ToString()];
                    }
                    else
                        confidence += 1;
                }
            }
            return confidence;
        }

        internal static string convertVectorToStringRPC(CameraSpacePoint v)
        {
            return "" + Math.Round(v.X, 3) + MessageSeparators.L3 + Math.Round(v.Y, 3) + MessageSeparators.L3 + Math.Round(v.Z, 3);
        }

        internal static string convertCameraDepthPointToStringRPC(DepthSpacePoint p)
        {
            return "" + Math.Round(p.X, 3) + MessageSeparators.L3 + Math.Round(p.Y, 3) + MessageSeparators.L3 + 0.0;
        }
    }
}