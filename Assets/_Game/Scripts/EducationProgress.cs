using System;using System.Collections.Generic;
namespace IanGame {
[Serializable]public class ExamRecord {public string id,educationId;public int day,score;public bool passed,reportReceived;}
[Serializable]public class EducationEnrollment {public string educationId;public int studies,homework;public bool graduated;}
[Serializable]public class EducationProgress {
 public string currentEducationId,careerId;public int books,studies,homework,experience,preparation;
 public List<EducationEnrollment> enrollments=new List<EducationEnrollment>();public List<ExamRecord> exams=new List<ExamRecord>();
}
[Serializable]public class RelationshipProgress {public string npcId;public int affinity,lastInteractionDay=-1,lastDecayDay;public bool friend;public RelationshipState state;}
[Serializable]public class SocialRecord {public string id,npcId;public int action,day;public bool success;}
[Serializable]public class SocialProgress {public int experience;public List<RelationshipProgress> relationships=new List<RelationshipProgress>();public List<SocialRecord> history=new List<SocialRecord>();}
[Serializable]public class WorkRecord {public string id,jobId;public int day,wage;public bool paid;}
[Serializable]public class EmploymentRecord {public string id,jobId,kind;public int day;public bool success;}
[Serializable]public class EmploymentProgress {
 public bool resumeWritten;public string currentJobId="unemployed",selectedJobId="convenience";public int workCount,experience,rank,strikes;
 public List<WorkRecord> work=new List<WorkRecord>();public List<EmploymentRecord> history=new List<EmploymentRecord>();
}
[Serializable]public class ActivityReceipt {public string id;public int action,day;}
[Serializable]public class Phase23Snapshot {
 public EducationProgress education=new EducationProgress();public SocialProgress social=new SocialProgress();public EmploymentProgress employment=new EmploymentProgress();
 public List<MissionItemKey> owned=new List<MissionItemKey>();public List<MissionItemKey> equipped=new List<MissionItemKey>();public List<string> deliveryIds=new List<string>();
 public List<ActivityReceipt> activities=new List<ActivityReceipt>();
}
}
