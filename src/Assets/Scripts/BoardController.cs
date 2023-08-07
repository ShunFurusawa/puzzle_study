using System.Collections;
using System.Collections.Generic;
using UnityEngine;

struct FallData
{
    public readonly int X { get; }
    public readonly int Y { get; }
    public readonly int Dest { get; }
    public FallData(int x, int y, int dest)
    {
        X = x;
        Y = y;
        Dest = dest;
    }
}

public class BoardController : MonoBehaviour
{
    public const int FALL_FRAME_PER_CELL = 5; //�P�ʃZ��������̗����t���[����
    public const int BOARD_WIDTH = 6;
    public const int BOARD_HEIGHT = 14;

    [SerializeField] GameObject prefabPuyo = default!;

    int[,] _board = new int[BOARD_HEIGHT, BOARD_WIDTH];
    GameObject[,] _Puyos = new GameObject[BOARD_HEIGHT, BOARD_WIDTH];

    //�����鎞�̈ꎞ�I�ϐ�
    List<FallData> _falls = new();
    int _fallFrames = 0;

    // �폜���鎞�̈ꎟ�I�ϐ�
    List<Vector2Int> _erases = new();
    int _eraseFrames = 0;

    private void ClearAll()
    {
        for (int y = 0; y < BOARD_HEIGHT; y++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                _board[y, x] = 0;

                if (_Puyos[y, x] != null) Destroy(_Puyos[y, x]);
                _Puyos[y, x] = null;
            }
        }
    }

    // Start is called before the first frame update
    public void Start()
    {
        ClearAll();
    }

    public static bool IsValidated(Vector2Int pos)
    {
        return 0 <= pos.x && pos.x < BOARD_WIDTH
            && 0 <= pos.y && pos.y < BOARD_HEIGHT;
    }

    public bool CanSettle(Vector2Int pos)
    {
        if (!IsValidated(pos)) return false;

        return 0 == _board[pos.y, pos.x];
    }

    public bool Settle(Vector2Int pos, int val)
    {
        if (!CanSettle(pos)) return false;

        _board[pos.y, pos.x] = val;

        Debug.Assert(_Puyos[pos.y, pos.x] == null);
        Vector3 world_position = transform.position + new Vector3(pos.x, pos.y, 0.0f);
        _Puyos[pos.y, pos.x] = Instantiate(prefabPuyo, world_position, Quaternion.identity, transform);
        _Puyos[pos.y, pos.x].GetComponent<PuyoController>().SetPuyoType((PuyoType)val);

        return true;
    }  

    public bool CheckFall()
    {
        _falls.Clear();
        _fallFrames = 0;

        //�������̍����̋L�^�p
        int[] dsts = new int[BOARD_WIDTH];
        for (int x = 0; x < BOARD_WIDTH; x++)
        { 
            dsts[x] = 0;
        }

        int max_check_line = BOARD_HEIGHT - 1;  //���͂Ղ�Ղ�2�ł͍ŏ�i�͗����Ă��Ȃ�
        for (int y = 0; y < max_check_line; y++)//�������Ɍ���
        {
            for (int x = 0; x <  BOARD_WIDTH; x++)
            {
                if (_board[y, x] == 0) continue;

                int dst = dsts[x];
                dsts[x] = y + 1;    //��̂Ղ悪�����Ă���Ȃ玩���̏�

                if (y == 0) continue; //��ԉ��Ȃ痎���Ȃ�

                if (_board[y - 1, x] != 0) continue; //��������ΑΏۊO

                _falls.Add(new FallData(x, y, dst));

                //�f�[�^��ύX���Ă���
                _board[dst,x] = _board[y, x];
                _board[y, x] = 0;
                _Puyos[dst, x] = _Puyos[y, x];
                _Puyos[y, x] = null;

                dsts[x] = dst + 1; //���̕��͗���������ɏ�ɏ��
            }
            
        }
        return _falls.Count != 0;
    }

    public bool Fall()
    {
        _fallFrames++;

        float dy = _fallFrames / (float)FALL_FRAME_PER_CELL;
        int di = (int)dy;

        for (int i = _falls.Count - 1; 0 <= i; i--) //���[�v���ō폜���Ă����S�Ȃ悤�Ɍ�납�猟��
        {
            FallData f = _falls[i];

            Vector3 pos = _Puyos[f.Dest, f.X].transform.localPosition;
            pos.y = f.Y - dy;

            if(f.Y <= f.Dest + di)
            {
                pos.y = f.Dest;
                _falls.RemoveAt(i);
            }
            _Puyos[f.Dest, f.X].transform.localPosition = pos; //�\���ʒu�̍X�V
        }           
        return _falls.Count != 0;
    }

    static readonly Vector2Int[] search_tbl = new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    // ������Ղ����������i�����F���㉺���E�Ɍ����Ă����B��������t���O�𗧂ĂčČv�Z���Ȃ��j

    public bool CheckErase()
    {
        _eraseFrames = 0;
        _erases.Clear();

        uint[] isChecked = new uint[BOARD_HEIGHT];�@// �������𑽂��g���͖̂��ʂȂ̂Ńr�b�g����

        List<Vector2Int> add_list = new();
        for (int y = 0; y < BOARD_HEIGHT; y++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if ((isChecked[y] & (1u << x)) != 0) continue;�@// �����ς�

                isChecked[y] |= (1u << x);

                int type = _board[y, x];
                if (type == 0) continue;�@// ��Ԃ�����

                System.Action<Vector2Int> get_connection = null;�@// �ċA�Ŏg���ꍇ�ɕK�v

                get_connection = (pos) =>
                {
                    add_list.Add(pos);�@// �폜�ΏۂƂ���

                    foreach (Vector2Int d in search_tbl)
                    {
                        Vector2Int target = pos + d;

                        if (target.x < 0 || BOARD_WIDTH <= target.x ||
                            target.y < 0 || BOARD_HEIGHT <= target.y) continue;// �͈͊O

                        if (_board[target.y, target.x] != type) continue;// �F�Ⴂ

                        if ((isChecked[target.y] & (1u << target.x)) != 0) continue;// �����ς�

                        isChecked[target.y] |= (1u << target.x);
                        get_connection(target);
                    }
                };

                add_list.Clear();
                get_connection(new Vector2Int(x, y));

                if (4 <= add_list.Count)
                {
                    _erases.AddRange(add_list);
                }
            }
        }

        return _erases.Count != 0;
    }

    public bool Erase()
    {
        _eraseFrames++;

        // 1���瑝���Ă�����Ƃ�����ő�ɑ傫���Ȃ������Ə������Ȃ��ď�����
        float t = _eraseFrames * Time.deltaTime;
        t = 1.0f - 10.0f * ((t - 0.1f) * (t - 0.1f) - 0.1f * 0.1f);

        // �傫�������Ȃ炨���܂�
        if (t <= 0.0f)
        {
            // �f�[�^�ƃQ�[���I�u�W�F�N�g�������ŏ���
            foreach (Vector2Int d in _erases)
            {
                Destroy(_Puyos[d.y, d.x]);
                _Puyos[d.y, d.x] = null;
                _board[d.y, d.x] = 0;
            }

            return false;
        }

        // ���f���̑傫����ς���
        foreach (Vector2Int d in _erases)
        {
            _Puyos[d.y, d.x].transform.localScale = Vector3.one * t;
        }

        return true;
    }
}