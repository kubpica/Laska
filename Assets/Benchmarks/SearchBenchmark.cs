using Laska;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SearchBenchmark : MonoBehaviour
{
    private static int testId = 0;

    public LaskaAI ai;

    private void resetScene()
    {
        PiecesManager.TempMoves = false;
        SceneManager.LoadScene("Laska");
    }

    private void failHard()
    {
        ai.cfg.failSoft = false;
        ai.GetComponent<TranspositionTable>().failSoft = false;
    }
    private void failSoft()
    {
        ai.cfg.failSoft = true;
        ai.GetComponent<TranspositionTable>().failSoft = true;
    }
    private void transpositionTable()
    {
        ai.cfg.useTranspositionTable = true;
        ai.cfg.useTTForDirectEvals = true;
    }
    private void alfaBeta() => ai.cfg.dontUseAlphaBeta = false;
    private void disableAlfaBeta() => ai.cfg.dontUseAlphaBeta = true;
    private void orderMoves() => ai.cfg.orderMoves = true;
    private void antyZugzwang() => ai.cfg.antyZugzwang = true;
    private void iterativeDeepening()
    {
        ai.cfg.useIterativeDeepening = true;
        ai.cfg.limitDeepeningDepth = true;
    }
    private void orderByRisk() 
    {
        orderMoves();
        var ordering = ai.cfg.GetComponentInParent<MoveOrdering>();
        ordering.evalStrengthByRisk = true;
        ordering.evalTakesByRisk = true;
    }
    private void classicOrdering()
    {
        orderMoves();
        var ordering = ai.cfg.GetComponentInParent<MoveOrdering>();
        ordering.evalStrengthByRisk = false;
        ordering.evalTakesByRisk = false;
    }

    private void test1()
    {
        Debug.LogError("Brak");
        disableAlfaBeta();
        failHard();
    }

    private void test2()
    {
        Debug.LogError("Tablica transpozycji");
        disableAlfaBeta();
        transpositionTable();
        failHard();
    }

    private void test3()
    {
        Debug.LogError("Odcinanie Alfa-beta");
        alfaBeta();
        failHard();
    }

    private void test4()
    {
        Debug.LogError("Alfa-beta, Fail-hard, TT");
        alfaBeta();
        transpositionTable();
        failHard();
    }

    private void test5()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT");
        alfaBeta();
        transpositionTable();
        failSoft();
    }

    private void test6()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (Simple)");
        alfaBeta();
        failSoft();
        transpositionTable();
        classicOrdering();
    }

    private void test7()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk)");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
    }

    private void test8()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk), Deepening");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
        iterativeDeepening();
    }

    private void test9()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (Strength by risk)");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderMoves();
        var ordering = ai.cfg.GetComponentInParent<MoveOrdering>();
        ordering.evalStrengthByRisk = true;
        ordering.evalTakesByRisk = false;
    }

    private void test10()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (Takes by risk)");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderMoves();
        var ordering = ai.cfg.GetComponentInParent<MoveOrdering>();
        ordering.evalStrengthByRisk = false;
        ordering.evalTakesByRisk = true;
    }

    private void test11()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk), Anty-zugzwang");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
        antyZugzwang();
    }

    private void test12()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk), Anty-zugzwang-seek-win");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
        antyZugzwang();
        ai.cfg.seekWinInZugzwangSearch = true;
    }

    private void test13()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk), Anty-zugzwang, Deepening");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
        antyZugzwang();
        iterativeDeepening();
    }

    private void test14()
    {
        Debug.LogError("Alfa-beta, Fail-soft, TT, Move ordering (ByRisk), Anty-zugzwang-seek-win, Deepening");
        alfaBeta();
        failSoft();
        transpositionTable();
        orderByRisk();
        antyZugzwang();
        ai.cfg.seekWinInZugzwangSearch = true;
        iterativeDeepening();
    }

    private void Start()
    {
        var temp1 = GameManager.Instance.ActivePlayer.AI;
        var temp2 = GameManager.Instance.ActivePlayer.AI;

        switch (++testId) 
        {
            case 1:
                test1();
                break;
            case 2:
                test2();
                break;
            case 3:
                test3();
                break;
            case 4:
                test4();
                break;
            case 5:
                test5();
                break;
            case 6:
                test6();
                break;
            case 7:
                test7();
                break;
            case 8:
                test8();
                break;
            case 9:
                test9();
                break;
            case 10:
                test10();
                break;
            case 11:
                test11();
                break;
            case 12:
                test12();
                break;
            case 13:
                test13();
                break;
            case 14:
                test14();
                break;
            default:
                return;
        }
        MoveMaker.Instance.onMoveStarted.AddListener(_ => resetScene());
        ai.MakeMove();
    }
}
