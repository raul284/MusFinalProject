using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public ParallaxBackground_0 _parallax;
    public Transform _player;
    public Animator _animator;
    public SpriteRenderer _spriteRenderer;
    public BackgroundControl_0 background;
    public Image _fadeImg;

    public float Speed = 4f;
    private float _speed;
    private float _exp = 0;
    private int dir = 1;

    public float JumpPower = 2f;
    private float _jump;
    private float _jumpExp;
    private float _playerPos;

    private bool movingLeft=false;
    private bool movingRight=false;
    private bool _running = false;
    private bool _isJumping = false;
    private bool _falling = false;

    private int _camDif = 1;
    private enum Zones { 
        Forest=0,
        Snow=1,
        Spokie=2,
        Dessert=3
    }
    private Zones _currZone = Zones.Forest;
    public float[] stepsPerZone = new float[4];
    public float[] zoneVolume = new float[4];

    private bool _Changing = false;
    private bool hasChanged = false;
    private int _changeZone = 0;
    private float _fadeTimer = 0;
    public float _FADE_DURATION;
    public float _ZONE_DISTANCE;
    public FMODUnity.StudioEventEmitter _musicEmitter;
    public FMODUnity.StudioEventEmitter _stepsEmitter;
    public FMODUnity.StudioEventEmitter _jumpEmitter;

    public bool AutoPlay = false;
    private float _autoTimer = 0;
    // Start is called before the first frame update
    void Start()
    {
        _speed=Speed;
        _jump = 0;
        _jumpExp = 1;
        zoneVolume[0] = 1;
        SetZonesVolume();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.P)) { SetAutoPlay(); }
        if((Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))&& !movingLeft) { SetMove(-1);}
        else if ((Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.A)) && movingLeft) { Stop(-1);}
        else if ((Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) && !movingRight) { SetMove(1); }
        else if ((Input.GetKeyUp(KeyCode.RightArrow) || Input.GetKeyUp(KeyCode.D)) && movingRight) { Stop(1);}

        if (Input.GetKeyDown(KeyCode.LeftShift) && !_running) Sprint(true);
        else if (Input.GetKeyUp(KeyCode.LeftShift) && _running) Sprint(false);

        if (Input.GetKeyDown(KeyCode.Space)){ SetJump();}

        if (AutoPlay)
        {
            _autoTimer += Time.deltaTime;
            if(_autoTimer>5 && Random.Range(0, 4) == 2)
            {
                SetJump();
                _autoTimer = 0;
            }
        }

        if(_isJumping) Jump();
        if (AutoPlay || movingLeft || movingRight) { 
            Moving();
            SetZonesVolume();
        }
        else
        {
            if (_animator.GetBool("Moving"))
            {
                if (_exp > 0)
                {
                    _player.position = new Vector3(_player.position.x + _exp * Speed * dir * Time.deltaTime, _player.position.y, _player.position.z);
                    _parallax.Camera_MoveSpeed = _exp*1.5f * Speed * dir;
                    _exp -= Time.deltaTime*2;
                }
                else
                {
                    _exp = 0;
                    _parallax.Camera_MoveSpeed = 0;
                    if ((_falling && !_isJumping) || (!_falling && !_isJumping))
                    {
                        _falling = false;
                        _animator.SetBool("Moving",false);
                        _stepsEmitter.EventInstance.setParameterByName("Steps", 0);
                    }
                }
            }
        }
        CameraFollow();
        if (_Changing)
        {
            Fade(true);
            if (Fade(true))
            {
                hasChanged = true;
                Change(_changeZone);
            }
        }
        else if (hasChanged)
        {
            if (Fade(false))
            {
                hasChanged = false;
                _changeZone = 0;
            }
        }
    }

    void SetAutoPlay()
    {
        AutoPlay = !AutoPlay;
        if (AutoPlay)
        {
            _autoTimer = 0;
            _animator.SetInteger("Dir", dir);
            _spriteRenderer.flipX = dir < 0;
            _animator.SetBool("Moving", true);
            _stepsEmitter.EventInstance.setParameterByName("Steps", 1);
        }
    }
    void CameraFollow()
    {
        if (Mathf.Abs(Camera.main.transform.position.x - _player.position.x) > 0.05f)
        {
            if (Camera.main.transform.position.x > _player.position.x) _camDif = -1;
            else _camDif = 1;
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x + _camDif * _speed * 0.1f * Time.deltaTime, Camera.main.transform.position.y, Camera.main.transform.position.z);
        }
    }
    void SetJump()
    {
        if (!_isJumping &&
            (_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Blink" ||
            _animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "Run"))
        {
            _isJumping = true;
            _jump = 0;
            _falling = false;
            _animator.SetTrigger("Jump");
            _animator.SetBool("Jumping", true);
            if (_animator.GetBool("Moving"))
            {
                if (_running) _jumpExp = 2;
                else _jumpExp = 1.5f;
                _stepsEmitter.EventInstance.setParameterByName("Steps",0);
            }
            else _jumpExp = 1;
            _playerPos = _player.position.y;
            _jumpEmitter.Play();
        }
    }
    void Jump()
    {
        if(!_falling) _player.position = new Vector3(_player.position.x, _player.position.y + (JumpPower - (0.5f - (0.5f-_jump)) * JumpPower) * Time.deltaTime * _jumpExp, _player.position.z);
        else _player.position = new Vector3(_player.position.x, _player.position.y - (JumpPower - (0.5f -_jump) * JumpPower) * Time.deltaTime * _jumpExp, _player.position.z);
        if (_jump >= 0.5f)
        {
            if (!_falling)
            {
                _falling = true;
                _jump = 0;
                _animator.SetTrigger("Fall");
            }
            else
            {
                _animator.SetBool("Jumping", false);
                _isJumping = false;
                _player.position = new Vector3(_player.position.x, _playerPos, _player.position.z);
                if (AutoPlay || movingLeft || movingRight) {
                    if(_running) _stepsEmitter.EventInstance.setParameterByName("Steps", 2);
                    else _stepsEmitter.EventInstance.setParameterByName("Steps", 1);
                }
                return;
            }
        }
        if (_jump < 0.5f) _jump += Time.deltaTime * 1.5f;
    }
    void SetMove(int _direction)
    {
        movingLeft = _direction < 0; 
        movingRight = _direction > 0; 
        dir = _direction;
        _animator.SetInteger("Dir", dir);
        _spriteRenderer.flipX = _direction < 0; 
        _animator.SetBool("Moving",true);
        if (_running) _stepsEmitter.EventInstance.setParameterByName("Steps", 2);
        else _stepsEmitter.EventInstance.setParameterByName("Steps", 1);
    }
    void Moving()
    {
        _player.position = new Vector3(_player.position.x + _exp * _speed * dir * Time.deltaTime, _player.position.y, _player.position.z);
        if (_exp < 1)
        {
            _parallax.Camera_MoveSpeed = _exp / 1.75f * _speed * dir;
            _exp += Time.deltaTime;
        }
        else _parallax.Camera_MoveSpeed = _speed * dir;
       if(!_Changing && _changeZone == 0) ChangeZone();
    }
    void ChangeZone()
    {
        stepsPerZone[_currZone.GetHashCode()] += _exp * _speed * dir * Time.deltaTime;
        if (stepsPerZone[_currZone.GetHashCode()] > (_ZONE_DISTANCE -25))
        {
            zoneVolume[_currZone.GetHashCode()] = 1 - (((stepsPerZone[_currZone.GetHashCode()] - (_ZONE_DISTANCE -25)) / 25)) * 0.25f;
            if(_currZone.GetHashCode()<zoneVolume.Length-1) zoneVolume[_currZone.GetHashCode()+1] = (stepsPerZone[_currZone.GetHashCode()] - (_ZONE_DISTANCE -25)) / 25 * 0.75f;
            else zoneVolume[0] = (stepsPerZone[_currZone.GetHashCode()] - (_ZONE_DISTANCE -25)) / 25 * 0.75f;

            if (stepsPerZone[_currZone.GetHashCode()] > _ZONE_DISTANCE)
            {
                stepsPerZone[_currZone.GetHashCode()] = _ZONE_DISTANCE;
                if (_currZone == Zones.Dessert) _currZone = 0;
                else _currZone++;
                stepsPerZone[_currZone.GetHashCode()] = -_ZONE_DISTANCE;
                _Changing = true;
                _changeZone = 1;
                _fadeTimer = 0;
            }
        }
        else if (stepsPerZone[_currZone.GetHashCode()] < -(_ZONE_DISTANCE - 25))
        {
            zoneVolume[_currZone.GetHashCode()] = 1 + ((stepsPerZone[_currZone.GetHashCode()] + (_ZONE_DISTANCE - 25)) / 25) * 0.25f;
            if(_currZone.GetHashCode()>0) zoneVolume[_currZone.GetHashCode()-1] = -((stepsPerZone[_currZone.GetHashCode()] + (_ZONE_DISTANCE - 25)) / 25) * 0.75f;
            else zoneVolume[zoneVolume.Length-1] = -((stepsPerZone[_currZone.GetHashCode()] + (_ZONE_DISTANCE - 25)) / 25) * 0.75f;

            if (stepsPerZone[_currZone.GetHashCode()] < -_ZONE_DISTANCE)
            {
                stepsPerZone[_currZone.GetHashCode()] = -_ZONE_DISTANCE;
                if (_currZone == Zones.Forest) _currZone = Zones.Dessert;
                else _currZone--;
                stepsPerZone[_currZone.GetHashCode()] = _ZONE_DISTANCE;
                _Changing = true;
                _changeZone = -1;
                _fadeTimer = 0;
            }
        }   
        else
        {
            for (int i = 0; i < zoneVolume.Length; i++)
            {
                if (_currZone.GetHashCode() == i) zoneVolume[i] = 1;
                else zoneVolume[i] = 0;
            }
        }
    }
    void SetZonesVolume()
    {
        _musicEmitter.EventInstance.setParameterByName("VolumeZone0", zoneVolume[0]);
        _musicEmitter.EventInstance.setParameterByName("VolumeZone1", zoneVolume[1]);
        _musicEmitter.EventInstance.setParameterByName("VolumeZone2", zoneVolume[2]);
        _musicEmitter.EventInstance.setParameterByName("VolumeZone3", zoneVolume[3]);

        _stepsEmitter.EventInstance.setParameterByName("VolumeZone0", zoneVolume[0]);
        _stepsEmitter.EventInstance.setParameterByName("VolumeZone1", zoneVolume[1]);
        _stepsEmitter.EventInstance.setParameterByName("VolumeZone2", zoneVolume[2]);
        _stepsEmitter.EventInstance.setParameterByName("VolumeZone3", zoneVolume[3]);
    }
    void Stop(int _direction)
    {
        if(_direction<0) movingLeft = false;
        else movingRight = false;
    }
    void Sprint(bool sprint)
    {
        _running = sprint;
        _animator.SetBool("Sprint", sprint);
        if (sprint)
        {
            _speed = Speed * 2f;
        }
        else _speed = Speed;
        if(AutoPlay || movingLeft || movingRight)
        {
            if (!_isJumping)
            {
                if(sprint) _stepsEmitter.EventInstance.setParameterByName("Steps", 2);
                else _stepsEmitter.EventInstance.setParameterByName("Steps", 1);
            }
        }
    }
    bool Fade(bool fade)
    {
        _fadeTimer += Time.deltaTime;
        if (fade)
        {
            _fadeImg.color = new Color(_fadeImg.color.r, _fadeImg.color.g, _fadeImg.color.b, _fadeImg.color.a + Time.deltaTime);
        }
        else
        {
            _fadeImg.color = new Color(_fadeImg.color.r, _fadeImg.color.g, _fadeImg.color.b, _fadeImg.color.a - Time.deltaTime);
        }
        if (_fadeTimer > _FADE_DURATION/2 && ((_fadeImg.color.a >= 1f && fade) || (_fadeImg.color.a <= 0f && !fade)))
        {
            return true;
        }
        else return false;
    }
    void Change(int direction)
    {
        if(direction>0)background.NextBG();
        else background.BackBG();
        _Changing = false;
        _fadeTimer = 0;
    }
}

