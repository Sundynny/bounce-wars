using UnityEngine;
using UnityEngine.UI;

public class MusicManager : MonoBehaviour
{
    public AudioSource musicSource;
    public Image musicButtonImage;   // Ảnh của nút nhạc
    public Sprite musicOnIcon;
    public Sprite musicOffIcon;

    private bool isMusicOn = true;

    void Start()
    {
        // Lấy trạng thái lưu trước đó (1 = bật, 0 = tắt)
        isMusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        UpdateMusicState();
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        PlayerPrefs.SetInt("MusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateMusicState();
    }

    private void UpdateMusicState()
    {
        if (isMusicOn)
        {
            musicSource.Play();
            if (musicButtonImage != null) musicButtonImage.sprite = musicOnIcon;
        }
        else
        {
            musicSource.Pause();
            if (musicButtonImage != null) musicButtonImage.sprite = musicOffIcon;
        }
    }
}
