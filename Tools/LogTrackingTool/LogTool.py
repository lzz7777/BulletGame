import os
import sys
import zipfile
import argparse
from datetime import datetime

# 阿里云 OSS SDK (需要先 pip install oss2)
try:
    import oss2
except ImportError:
    print("未安装 oss2 库。请先执行: pip install oss2")
    sys.exit(1)

# ==========================================
# 1. 阿里云 OSS 配置 (请替换为你的真实配置)
# ==========================================
ACCESS_KEY_ID = 'your_access_key_id'
ACCESS_KEY_SECRET = 'your_access_key_secret'
ENDPOINT = 'your_oss_endpoint'  # 例如: oss-cn-hangzhou.aliyuncs.com
BUCKET_NAME = 'your_bucket_name' # 例如: Bucket_HotFixeBundle

# 本地临时下载与解压目录
LOCAL_CACHE_DIR = "./log_cache"

class LogTool:
    def __init__(self):
        # 初始化 OSS 客户端
        auth = oss2.Auth(ACCESS_KEY_ID, ACCESS_KEY_SECRET)
        self.bucket = oss2.Bucket(auth, ENDPOINT, BUCKET_NAME)
        
        if not os.path.exists(LOCAL_CACHE_DIR):
            os.makedirs(LOCAL_CACHE_DIR)

    def download_logs(self, game_name, platform_name, player_id, date_prefix):
        """
        从 OSS 下载指定前缀的日志 (包括 Crash 和 Manual 压缩包)
        """
        # 根据我们之前定的 C# 格式拼接 OSS 根目录
        oss_prefix = f"Report/{game_name}/{platform_name}/{player_id}/"
        print(f"[*] 开始扫描 OSS 目录: {oss_prefix} ...")

        downloaded_files = []
        
        # 列出该玩家目录下的所有文件
        for obj in oss2.ObjectIterator(self.bucket, prefix=oss_prefix):
            # 如果指定了日期前缀 (例如 20250904)，则只下载包含该日期的文件
            if date_prefix and date_prefix not in obj.key:
                continue

            # 忽略目录本身
            if obj.key.endswith('/'):
                continue

            # 构造本地保存路径 (保留 OSS 上的目录结构)
            local_path = os.path.join(LOCAL_CACHE_DIR, obj.key.replace('/', os.sep))
            local_dir = os.path.dirname(local_path)
            if not os.path.exists(local_dir):
                os.makedirs(local_dir)

            print(f"  -> 下载: {obj.key} => {local_path}")
            self.bucket.get_object_to_file(obj.key, local_path)
            downloaded_files.append(local_path)

        if not downloaded_files:
            print(f"[!] 未找到匹配的日志文件 (Date: {date_prefix})")
        else:
            print(f"[*] 共下载了 {len(downloaded_files)} 个文件。")
            
        return downloaded_files

    def extract_zips(self, file_paths):
        """
        找出所有的 .zip 文件并解压到同名文件夹下，返回所有可用于搜索的 .txt 文件路径
        """
        txt_files = []
        for file_path in file_paths:
            if file_path.endswith('.zip'):
                extract_dir = file_path[:-4] # 砍掉 .zip 作为文件夹名
                print(f"[*] 解压 Zip: {file_path} => {extract_dir}/")
                with zipfile.ZipFile(file_path, 'r') as zip_ref:
                    zip_ref.extractall(extract_dir)
                
                # 把解压出来的 txt 文件加入列表
                for root, _, files in os.walk(extract_dir):
                    for f in files:
                        if f.endswith('.txt'):
                            txt_files.append(os.path.join(root, f))
            elif file_path.endswith('.txt'):
                txt_files.append(file_path)
                
        return txt_files

    def search_keyword(self, txt_files, keyword, context_lines=5):
        """
        在所有解压后的文本文件中搜索关键字，并打印出上下 N 行上下文
        """
        print(f"\n{'='*50}")
        print(f"[*] 开始搜索关键字: '{keyword}' (上下文 {context_lines} 行)")
        print(f"{'='*50}\n")
        
        hit_count = 0
        
        for file_path in txt_files:
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    lines = f.readlines()
            except Exception as e:
                print(f"[!] 无法读取文件 {file_path}: {e}")
                continue

            # 记录该文件中命中的行号
            hit_indices = [i for i, line in enumerate(lines) if keyword in line]
            
            if not hit_indices:
                continue
                
            hit_count += len(hit_indices)
            print(f"\n>>> 发现命中 [{len(hit_indices)}次] 于文件: {file_path} <<<")
            
            # 打印包含上下文的内容
            last_printed = -1
            for idx in hit_indices:
                start = max(0, idx - context_lines)
                end = min(len(lines), idx + context_lines + 1)
                
                # 避免上下文重叠打印
                start = max(start, last_printed + 1)
                if start >= end:
                    continue
                    
                print("-" * 40)
                for i in range(start, end):
                    prefix = ">> " if i == idx else "   "
                    # 去掉末尾换行符打印
                    print(f"{prefix}[Line {i+1:04d}] {lines[i].rstrip()}")
                
                last_printed = end - 1

        print(f"\n{'='*50}")
        print(f"[*] 搜索完毕。总共找到 {hit_count} 处匹配。")

def main():
    parser = argparse.ArgumentParser(description="BulletGame 日志排查工具 (从 OSS 下载 -> 解压 -> 搜索)")
    
    # 必填参数
    parser.add_argument('-i', '--id', required=True, help="玩家/主播 OpenID")
    parser.add_argument('-k', '--keyword', required=True, help="要搜索的关键字 (例如 'NullReferenceException' 或 '结算')")
    
    # 选填参数
    parser.add_argument('-d', '--date', default='', help="日期前缀过滤 (格式: yyyyMMdd，如 20250904)。不填则拉取该玩家所有历史日志")
    parser.add_argument('-g', '--game', default='BulletGame', help="游戏名称 (默认: BulletGame)")
    parser.add_argument('-p', '--platform', default='DouYin', help="平台名称 (默认: DouYin)")
    parser.add_argument('-c', '--context', type=int, default=5, help="打印匹配行的上下 N 行上下文 (默认: 5)")
    
    args = parser.parse_args()

    tool = LogTool()
    
    # 1. 从 OSS 下载该玩家的日志
    downloaded_files = tool.download_logs(args.game, args.platform, args.id, args.date)
    
    if not downloaded_files:
        return

    # 2. 解压所有的 Zip 包，提取所有的 Txt 文件
    txt_files = tool.extract_zips(downloaded_files)
    
    # 3. 在所有文本文件中全文检索并打印上下文
    if txt_files:
        tool.search_keyword(txt_files, args.keyword, args.context)
    else:
        print("[!] 压缩包内没有找到可供搜索的 .txt 文件。")

if __name__ == "__main__":
    main()