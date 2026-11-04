using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using CercleVR;
using CercleVR.Core;
public static class PlayCampaignCheck {
 public static async Task<object> Main() {
 var game=UnityEngine.Object.FindAnyObjectByType<CercleGame>();
 var flags=BindingFlags.NonPublic|BindingFlags.Instance;
 Func<string,object> field=n=>typeof(CercleGame).GetField(n,flags).GetValue(game);
 Action<string> call=n=>typeof(CercleGame).GetMethod(n,flags).Invoke(game,null);
 var progress=(CampaignProgress)field("campaign"); int victories=0; int startFrame=Time.frameCount;
 for(int level=progress.LevelNumber;level<=progress.TotalLevels;level++) {
  var session=(Session)field("session"); if(session==null || session.Outcome!=Outcome.Playing)throw new Exception("Session not ready");
  typeof(CercleGame).GetMethod("Select",flags).Invoke(game,new object[]{0});
  call("Mark"); if(session.Marks[0]!=1)throw new Exception("Mark");
  call("Accuse"); call("Cancel"); if((int)field("pending")!=-1)throw new Exception("Cancel");
  call("ValidateLevelForEditor"); if(session.Outcome!=Outcome.Won)throw new Exception("Victory"); victories++;
  if(level<progress.TotalLevels) {
   float deadline=Time.realtimeSinceStartup+10;
   while(progress.LevelNumber==level || (bool)field("generating")) { if(Time.realtimeSinceStartup>deadline)throw new Exception("Advance timeout"); await Task.Delay(100); }
  }
 }
 var door=(Transform)field("doorPivot");
 if(!progress.Completed || Mathf.Abs(Mathf.DeltaAngle(door.localEulerAngles.y,-100))>0.1f)throw new Exception("Final door");
 return new {scene=game.gameObject.scene.path,victories,completed=progress.Completed,doorAngle=door.localEulerAngles.y,framesAdvanced=Time.frameCount-startFrame};
 }
}
