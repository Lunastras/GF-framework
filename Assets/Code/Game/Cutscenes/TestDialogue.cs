using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDialogue : GfgVnScene
{
    protected override void Begin()
    {
        Environment(GfcSceneId.ENV_MUSUEM);

        Say("I suck dick", new(StoryCharacter.PROTAG));
        Say();

        Say(new CornDialogueSetting(StoryCharacter.GF));

        Say("............Well... this is awkward...", new CornDialogueSetting(StoryCharacter.GF));

        SayKey("MuhDickTest", new(StoryCharacter.GF));
        {
            Append(" Cool dick");

            Option(Test1Num);
            Option(Test2Num);
            Option(Test3Num);
            {
                Append(" neah dick too small to press this");
            }
        }

        Wait(1.0f);

        Say("Pretty cool choice...");
        Say("Pretty cool choice1...");
        Say("Pretty cool choice2...");
        Say("Pretty cool choice3...");
        Say("Pretty cool choice4...");
    }

    void Test1Num()
    {
        Environment(GfcSceneId.ENV_PARK);

        Say(StoryCharacter.GF);
        SayKey("Lmao");

        Say(StoryCharacter.DUNNO);
        Option(Test2Num);
    }

    void Test2Num()
    {
        Say();
        Next(Test3Num);
    }

    void Test3Num()
    {
        Environment(GfcSceneId.ENV_RESTAURANT);
        Say(new CornDialogueSetting(StoryCharacter.TEST));
        Say();
    }
}