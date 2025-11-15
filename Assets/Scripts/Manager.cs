using System.Collections.Generic;
using UnityEngine;

public class Manager : MonoBehaviour
{
    [SerializeField] private VideoController videoController;

    [SerializeField] private Map map;

    [SerializeField] private GameObject[] blockPrefabs = new GameObject[0];

    [SerializeField] private RectTransform blockRoot;

    [SerializeField] private float blockFallInterval = 0;

    [SerializeField] private float blockLandingDuration = 0;

    [SerializeField] private float blockEraseInterval = 0;

    [SerializeField] private float blockEraseWaitDuration = 0;

    [SerializeField] private AudioSource seBlockLanding;

    [SerializeField] private AudioSource seBlockErase;

    [SerializeField] private AudioSource seGameOver;

    [SerializeField] TMPro.TextMeshProUGUI pointText;

    [SerializeField] private float inputRepeatFirstDuration = 0f;
    [SerializeField] private float inputRepeatDefaultDuration = 0f;

    [SerializeField] private Transform startPanel;

    [SerializeField] private UnityEngine.UI.Button startButton;
    [SerializeField] private UnityEngine.UI.Image startButtonImage;

    [SerializeField] private Color startButtonColor1;
    [SerializeField] private Color startButtonColor2;
    [SerializeField] private float startButtonColorDuration;

    private enum Phase
    {
        初期化,
        スタート待ち,

        ブロック出現,
        ブロック落下,
        ブロック着地,
        ブロック着地ウェイト,

        ブロック消去,
        ブロック消去ウェイト,

        ゲームオーバー開始,
        ゲームオーバーループ,

        テストパターン_初期化,
        テストパターン_ループ,
    }

    private Phase phase = Phase.初期化;

    private float phaseElapsed = 0f;

    private Block[,] blocks;

    private Block currentBlock = null;

    private Vector2Int currentBlockPosition;

    private float blockFallElapsed = 0;

    private List<(Vector2Int, Block, int)> eraseBlocks = new();

    private int blockEraseDepth = 0;
    private float blockEraseElapsed = 0f;
    private int blockErasePoint = 0;

    private bool inputEnable = false;
    private int inputCount = 0;
    private Vector2 inputValue;
    private float inputRepeatDuration = 0f;
    private float inputRepeatElapsed = 0f;

    private readonly Block.Type[] ERASE_PATTERNS = new Block.Type[]
    {
        Block.Type.コ,
        Block.Type.ン,
        Block.Type.コ,

        Block.Type.コ,
        Block.Type.ン,

        Block.Type.コ,
        Block.Type.ン,
        Block.Type.コ,

        Block.Type.コ,
        Block.Type.ン,

        Block.Type.コ,
        Block.Type.ン,

        Block.Type.コ,
        Block.Type.ン,
    };

    private class PatternLinkList
    {
        public readonly int depth;
        public readonly Vector2Int position;
        public readonly PatternLinkList parent;
        public readonly List<PatternLinkList> childs = new();

        public PatternLinkList(PatternLinkList parent, int depth, int x, int y)
        {
            this.parent = parent;
            this.depth = depth;
            position.x = x;
            position.y = y;
        }
    }

    private void Update()
    {
        phaseElapsed += Time.deltaTime;

        switch (phase)
        {
            case Phase.初期化:
                blocks = new Block[map.Width, map.Height];

                pointText.text = "0";

                startButton.onClick.AddListener(() =>
                {
                    startPanel.gameObject.SetActive(false);

                    videoController.Play();

                    SetPhase(Phase.ブロック出現);
                    // SetPhase(Phase.テストパターン_初期化);
                });
                SetPhase(Phase.スタート待ち);
                break;

            case Phase.スタート待ち:
                startButtonImage.color = Color.Lerp(startButtonColor1, startButtonColor2, (Mathf.Cos(phaseElapsed * startButtonColorDuration * 2 * Mathf.PI) + 1f) / 2f);
                break;

            case Phase.ブロック出現:
                {
                    var go = Instantiate(blockPrefabs[Random.Range(0, blockPrefabs.Length)], blockRoot);
                    currentBlock = go.GetComponent<Block>();
                    currentBlockPosition.Set(map.Width / 2, 0);
                    currentBlock.SetPosition(map.GetGrid(currentBlockPosition));
                }

                if (blocks[currentBlockPosition.x, currentBlockPosition.y] == null)
                {
                    SetPhase(Phase.ブロック落下);
                }
                else
                {
                    SetPhase(Phase.ゲームオーバー開始);
                }
                break;

            case Phase.ブロック落下:
                // 入力
                if (inputEnable)
                {
                    inputRepeatElapsed += Time.deltaTime;

                    var input = false;
                    if (inputCount == 0)
                    {
                        input = true;
                        inputRepeatDuration = inputRepeatFirstDuration;
                    }
                    else
                    {
                        if (inputRepeatElapsed >= inputRepeatDuration)
                        {
                            input = true;
                            inputRepeatDuration = inputRepeatDefaultDuration;
                        }
                    }

                    if (input)
                    {
                        inputCount++;
                        inputRepeatElapsed = 0;

                        // 左右移動
                        if (inputValue.x != 0)
                        {
                            var xadd = (int)Mathf.Sign(inputValue.x);
                            var xmov =
                                currentBlockPosition.x + xadd >= 0 &&
                                currentBlockPosition.x + xadd < map.Width &&
                                blocks[currentBlockPosition.x + xadd, currentBlockPosition.y] == null;
                            if (xmov)
                            {
                                currentBlockPosition.x += xadd;
                                currentBlock.SetPosition(map.GetGrid(currentBlockPosition));
                            }
                        }

                        // 下に落ちる
                        if (inputValue.y < 0)
                        {
                            blockFallElapsed = blockFallInterval;
                        }

                    }
                }

                // 落下
                blockFallElapsed += Time.deltaTime;
                if (blockFallElapsed >= blockFallInterval)
                {
                    blockFallElapsed = 0f;

                    var landing =
                        (currentBlockPosition.y + 1 >= map.Height) ||   // 最下層にいる
                        blocks[currentBlockPosition.x, currentBlockPosition.y + 1] != null; // 下にブロックがいる

                    // 着地した
                    if (landing)
                    {
                        SetPhase(Phase.ブロック着地);
                    }
                    // 着地していない
                    else
                    {
                        currentBlockPosition.y += 1;
                        currentBlock.SetPosition(map.GetGrid(currentBlockPosition));
                    }
                }
                break;

            case Phase.ブロック着地:
                if (currentBlock)
                {
                    blocks[currentBlockPosition.x, currentBlockPosition.y] = currentBlock;
                    seBlockLanding.Play();
                    currentBlock = null;
                }

                CheckEraseBlock();
                if (eraseBlocks.Count <= 0)
                {
                    SetPhase(Phase.ブロック着地ウェイト);
                }
                else
                {
                    SetPhase(Phase.ブロック消去);
                }

                break;

            case Phase.ブロック着地ウェイト:
                if (phaseElapsed >= blockLandingDuration)
                {
                    SetPhase(Phase.ブロック出現);
                }
                break;

            case Phase.ブロック消去:
                blockEraseElapsed += Time.deltaTime;
                if (blockEraseElapsed >= blockEraseInterval)
                {
                    // ブロックを順番に消す
                    if (eraseBlocks.Count > 0)
                    {
                        blockEraseElapsed = 0f;

                        for (var i = eraseBlocks.Count - 1; i >= 0; i--)
                        {
                            var (pos, block, depth) = eraseBlocks[i];
                            if (!blocks[pos.x, pos.y])
                            {
                                eraseBlocks.RemoveAt(i);
                                continue;
                            }

                            if (depth == blockEraseDepth)
                            {
                                eraseBlocks.RemoveAt(i);

                                block.Erase();
                                blocks[pos.x, pos.y] = null;

                                seBlockErase.Play();
                            }
                        }

                        blockEraseDepth++;
                    }
                    // ブロックを下に落とす
                    else
                    {
                        var loop = true;
                        var update = false;
                        while (loop)
                        {
                            loop = false;
                            for (var x = 0; x < map.Width; x++)
                            {
                                for (var y = map.Height - 2; y >= 0; y--)
                                {
                                    if (blocks[x, y] && !blocks[x, y + 1])
                                    {
                                        loop |= true;
                                        update |= true;
                                        blocks[x, y + 1] = blocks[x, y];
                                        blocks[x, y] = null;
                                    }
                                }
                            }
                        }

                        if (update)
                        {
                            for (var x = 0; x < map.Width; x++)
                            {
                                for (var y = 0; y < map.Height; y++)
                                {
                                    if (blocks[x, y])
                                    {
                                        blocks[x, y].SetPosition(map.GetGrid(x, y));
                                    }
                                }
                            }
                        }

                        // ポイント加算
                        pointText.text = $"{blockErasePoint}";

                        SetPhase(Phase.ブロック消去ウェイト);
                    }
                }
                break;

            case Phase.ブロック消去ウェイト:
                if (phaseElapsed >= blockEraseWaitDuration)
                {
                    SetPhase(Phase.ブロック着地);
                }
                break;

            case Phase.ゲームオーバー開始:
                seGameOver.Play();
                SetPhase(Phase.ゲームオーバーループ);
                break;

            case Phase.ゲームオーバーループ:
                break;

            case Phase.テストパターン_初期化:
                for (var x = 0; x < map.Width; x++)
                {
                    for (var y = 0; y < map.Height; y++)
                    {
                        if (TEST_PATTERN_001[y, x] == 田)
                        {
                            continue;
                        }

                        var go = Instantiate(blockPrefabs[TEST_PATTERN_001[y, x]], blockRoot);
                        var block = go.GetComponent<Block>();
                        block.SetPosition(map.GetGrid(x, y));
                        blocks[x, y] = block;
                    }
                }

                CheckEraseBlock();
                if (eraseBlocks.Count > 0)
                {
                    SetPhase(Phase.ブロック消去);
                }
                else
                {
                    SetPhase(Phase.テストパターン_ループ);
                }
                break;

            case Phase.テストパターン_ループ:
                break;
        }
    }

    private void SetPhase(Phase nextPhase)
    {
        Debug.LogWarning($"Phase: {phase} => {nextPhase}");
        phase = nextPhase;
        phaseElapsed = 0f;
        blockFallElapsed = 0f;
    }

    private void CheckEraseBlock()
    {
        void CheckRecursiveImpl(PatternLinkList parent, int depth, int x, int y, PatternLinkList[,] checkGrids, List<PatternLinkList> checkLists)
        {
            // グリッド外
            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height)
            {
                return;
            }

            if (blocks[x, y] == null)
            {
                return;
            }

            if (checkGrids[x, y] != null)
            {
                return;
            }

            if (ERASE_PATTERNS[depth] != blocks[x, y].BlockType)
            {
                return;
            }

            // 該当グリッドはOK
            var cur = new PatternLinkList(parent, depth, x, y);
            if (checkGrids[x, y] != null)
            {
                checkGrids[x, y].parent.childs.Remove(checkGrids[x, y]);
                checkLists.Remove(checkGrids[x, y]);
            }
            checkGrids[x, y] = cur;
            checkLists.Add(cur);
            parent.childs.Add(cur);

            // パターンの終端にきている
            if (depth + 1 >= ERASE_PATTERNS.Length)
            {
                return;
            }

            // 四方のグリッドを再帰的にチェック
            CheckRecursiveImpl(cur, depth + 1, x + 1, y, checkGrids, checkLists);
            CheckRecursiveImpl(cur, depth + 1, x - 1, y, checkGrids, checkLists);
            CheckRecursiveImpl(cur, depth + 1, x, y + 1, checkGrids, checkLists);
            CheckRecursiveImpl(cur, depth + 1, x, y - 1, checkGrids, checkLists);
        }

        // 消去パターンの連結リストの終端を集める
        var termLists = new List<PatternLinkList>();
        {
            // マップグリッドを走査
            for (var x = 0; x < blocks.GetLength(0); x++)
            {
                for (var y = blocks.GetLength(1) - 1; y >= 0; y--)
                {
                    if (blocks[x, y] == null || blocks[x, y].BlockType != ERASE_PATTERNS[0])
                    {
                        continue;
                    }

                    // 消去できるパターンを再帰的にチェック
                    var checkGrids = new PatternLinkList[blocks.GetLength(0), blocks.GetLength(1)];
                    var checkLists = new List<PatternLinkList>();
                    var root = new PatternLinkList(null, -1, x, y);
                    CheckRecursiveImpl(root, 0, x, y, checkGrids, checkLists);

                    // 終端を集める
                    for (var i = 0; i < checkLists.Count; i++)
                    {
                        var l = checkLists[i];
                        if (l.depth == ERASE_PATTERNS.Length - 1 && l.childs.Count == 0)
                        {
                            // Debug.LogWarning($"{l.depth}, {l.position}");
                            termLists.Add(l);
                        }
                    }
                }
            }
        }

        // 消去できるブロックをリストアップ
        eraseBlocks.Clear();
        {
            var checkGrids = new PatternLinkList[blocks.GetLength(0), blocks.GetLength(1)];
            for (var i = 0; i < termLists.Count; i++)
            {
                var term = termLists[i];

                // 配置できるかをまずチェック
                var enable = true;
                {
                    var link = term;
                    while (link.depth >= 0)
                    {
                        if (checkGrids[link.position.x, link.position.y] != null && checkGrids[link.position.x, link.position.y] != link)
                        {
                            enable = false;
                            break;
                        }
                        link = link.parent;
                    }
                }

                // 配置
                if (enable)
                {
                    var link = term;
                    while (link.depth >= 0)
                    {
                        // Debug.LogWarning($"[{i}] {link.position}, {link.depth}, {blocks[link.position.x, link.position.y].BlockType}");
                        checkGrids[link.position.x, link.position.y] = link;
                        var block = blocks[link.position.x, link.position.y];
                        eraseBlocks.Add(new(link.position, block, link.depth));
                        if (link.depth == ERASE_PATTERNS.Length - 1)
                        {
                            blockErasePoint ++;
                        }
                        link = link.parent;
                    }
                }
            }
        }
    }

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (Time.time < 0.5)
        {
            return;
        }

        if (context.started)
        {
            inputEnable = true;
            inputValue = context.ReadValue<Vector2>();
        }
        else if (context.canceled)
        {
            inputEnable = false;
            inputValue = Vector2.zero;
        }
        else
        {
            inputValue = context.ReadValue<Vector2>();
        }

        blockEraseDepth = 0;
        blockEraseElapsed = 0f;

        inputCount = 0;
        inputRepeatElapsed = 0;
    }

    private const int コ = (int)Block.Type.コ;
    private const int ン = (int)Block.Type.ン;
    private const int 田 = -1;

    private readonly int[,] TEST_PATTERN_001 = new int[12, 6]
    {
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, 田, 田, 田, 田 },
        { 田, 田, ン, コ, 田, 田 },
        { コ, ン, コ, ン, 田, 田 },
        { ン, コ, ン, コ, コ, ン },
        { コ, ン, コ, コ, ン, コ },
    };
}
