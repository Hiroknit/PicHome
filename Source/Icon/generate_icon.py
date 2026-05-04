#!/usr/bin/env python3
"""
PicHome写真整理アイコン生成スクリプト
写真整理をコンセプトにしたアイコンを生成します
"""

from PIL import Image, ImageDraw
import os

def generate_icon():
    # プロジェクトディレクトリパス
    script_dir = os.path.dirname(os.path.abspath(__file__))
    output_ico = os.path.join(script_dir, "App.ico")
    output_png = os.path.join(script_dir, "App.png")
    
    # アイコンサイズ
    size = 256
    
    # 新しいRGBA画像を作成（背景透過）
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # 色定義
    primary_blue = (0, 120, 212)      # #0078D4
    light_blue = (32, 150, 230)       # ライトブルー
    accent_color = (80, 227, 194)     # #50E3C2
    white = (255, 255, 255)
    
    # 背景円を描画（ライトブルー）
    margin = 10
    bg_box = [margin, margin, size - margin, size - margin]
    draw.ellipse(bg_box, fill=light_blue)
    
    # フォルダの形を描画（深いブルー）
    # フォルダタブ
    tab_width = 60
    tab_height = 25
    draw.rectangle(
        [size // 4, size // 3 - tab_height, size // 4 + tab_width, size // 3],
        fill=primary_blue
    )
    
    # フォルダメイン部分
    folder_top = size // 3
    folder_left = size // 4 - 10
    folder_right = 3 * size // 4
    folder_bottom = 2 * size // 3 + 20
    
    draw.rectangle(
        [folder_left, folder_top, folder_right, folder_bottom],
        fill=primary_blue,
        outline=white,
        width=3
    )
    
    # カメラレンズ（アクセント）を描画
    camera_center_x = size // 2
    camera_center_y = (folder_top + folder_bottom) // 2 + 15
    camera_radius = 20
    
    # レンズ外側
    draw.ellipse(
        [camera_center_x - camera_radius, camera_center_y - camera_radius,
         camera_center_x + camera_radius, camera_center_y + camera_radius],
        fill=accent_color,
        outline=white,
        width=2
    )
    
    # レンズ内側
    inner_radius = 12
    draw.ellipse(
        [camera_center_x - inner_radius, camera_center_y - inner_radius,
         camera_center_x + inner_radius, camera_center_y + inner_radius],
        fill=primary_blue
    )
    
    # レンズの内側ハイライト
    highlight_radius = 6
    draw.ellipse(
        [camera_center_x - highlight_radius - 3, camera_center_y - highlight_radius - 3,
         camera_center_x - 3, camera_center_y - 3],
        fill=accent_color
    )
    
    # PNG として保存
    # RGB画像に変換してPNG保存
    rgb_img = Image.new('RGB', (size, size), (245, 245, 245))
    rgb_img.paste(img, (0, 0), img)
    rgb_img.save(output_png, 'PNG')
    print(f"✓ PNG icon created: {output_png}")
    
    # ICOファイルを生成（複数サイズを含む）
    icon_sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)]
    icon_images = []
    
    for icon_size in icon_sizes:
        resized = rgb_img.resize(icon_size, Image.Resampling.LANCZOS)
        icon_images.append(resized)
    
    # ICOファイルとして保存
    # 最初のサイズ（最大解像度）をベースに、全てのサイズから構成
    icon_images[0].save(
        output_ico,
        'ICO',
        sizes=icon_sizes
    )
    print(f"✓ ICO icon created: {output_ico}")
    
    return output_ico, output_png

if __name__ == "__main__":
    try:
        ico_path, png_path = generate_icon()
        print("\n✅ アイコン生成成功！")
        print(f"   ICO: {ico_path}")
        print(f"   PNG: {png_path}")
    except Exception as e:
        print(f"❌ エラー: {e}")
        exit(1)
