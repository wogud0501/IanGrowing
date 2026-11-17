using System;
using System.Collections.Generic;

namespace IanGame
{
    [Serializable]
    public class ExamRecord
    {
        public string id;
        public string educationId;
        public int day;
        public int score;
        public bool passed;
        public bool reportReceived;
    }

    [Serializable]
    public class EducationEnrollment
    {
        public string educationId;
        public int studies;
        public int homework;
        public bool graduated;
    }

    [Serializable]
    public class EducationProgress
    {
        public string currentEducationId;
        public string careerId;
        public int books;
        public int studies;
        public int homework;
        public int experience;
        public int preparation;
        public List<EducationEnrollment> enrollments = new List<EducationEnrollment>();
        public List<ExamRecord> exams = new List<ExamRecord>();
    }

    [Serializable]
    public class RelationshipProgress
    {
        public string npcId;
        public int affinity;
        public int lastInteractionDay = -1;
        public int lastDecayDay;
        public bool friend;
        public RelationshipState state;
    }

    [Serializable]
    public class SocialRecord
    {
        public string id;
        public string npcId;
        public int action;
        public int day;
        public bool success;
    }

    [Serializable]
    public class SocialProgress
    {
        public int experience;
        public List<RelationshipProgress> relationships = new List<RelationshipProgress>();
        public List<SocialRecord> history = new List<SocialRecord>();
    }

    [Serializable]
    public class WorkRecord
    {
        public string id;
        public string jobId;
        public int day;
        public int wage;
        public bool paid;
    }

    [Serializable]
    public class EmploymentRecord
    {
        public string id;
        public string jobId;
        public string kind;
        public int day;
        public bool success;
    }

    [Serializable]
    public class EmploymentProgress
    {
        public bool resumeWritten;
        public string currentJobId = "unemployed";
        public string selectedJobId = "convenience";
        public int workCount;
        public int experience;
        public int rank;
        public int strikes;
        public List<WorkRecord> work = new List<WorkRecord>();
        public List<EmploymentRecord> history = new List<EmploymentRecord>();
    }

    [Serializable]
    public class ActivityReceipt
    {
        public string id;
        public int action;
        public int day;
    }

    [Serializable]
    public class Phase23Snapshot
    {
        public EducationProgress education = new EducationProgress();
        public SocialProgress social = new SocialProgress();
        public EmploymentProgress employment = new EmploymentProgress();
        public List<MissionItemKey> owned = new List<MissionItemKey>();
        public List<MissionItemKey> equipped = new List<MissionItemKey>();
        public List<string> deliveryIds = new List<string>();
        public List<ActivityReceipt> activities = new List<ActivityReceipt>();
    }
}
