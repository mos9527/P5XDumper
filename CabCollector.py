import UnityPy, os, logging, pickle
from collections import defaultdict
from multiprocessing.pool import ThreadPool
import shutil
import argparse

parser = argparse.ArgumentParser()
parser.add_argument('--rebuild', action='store_true', help='Rebuild cache')
parser.add_argument('--path', default='.', help='Path to search for cabs when rebuilding cache')
parser.add_argument('--dest', default='./packages', help='Destination folder')
parser.add_argument('load', nargs='*', default='', help='Input filenames')
args = parser.parse_args()
if not args.load and not args.rebuild:
    parser.print_help()
    exit(1)

logger = logging.getLogger()
logging.basicConfig(level=logging.INFO)

def cab_name_normalize(cab_name : str) -> str:
    return cab_name.lower()

def walk_files(source_folder : str):
    # iterate over all files in source folder
    for root, dirs, files in os.walk(source_folder):
        for file_name in files:
            yield os.path.join(root, file_name)

# These are actually thread-safe thanks to GIL
cab_to_filename = defaultdict(set)
cab_dependencies = defaultdict(set)

def walk_cabs(source_folder : str):
    def walk_inner(file_path):
        logger.info(f'Processing {file_path}')        
        env = UnityPy.load(file_path)
        for cab in env.cabs:
            logger.info(f'Found cab: {cab} -> {file_path}')
            cab_to_filename[cab_name_normalize(cab)].add(file_path)
            for ext in env.cabs[cab].externals:
                cab_dependencies[cab_name_normalize(cab)].add(cab_name_normalize(ext.name))
    with ThreadPool(processes=32) as pool:
        for file_path in walk_files(source_folder):            
            pool.apply_async(walk_inner, (file_path,))    
        pool.close()
        pool.join()    
    logger.info('All files processed. Saving cache.')
    pickle.dump(cab_to_filename, open('cab_to_filename.pkl', 'wb'))
    pickle.dump(cab_dependencies, open('cab_dependencies.pkl', 'wb'))    

if args.rebuild:
    walk_cabs(args.path)
else:
    logger.info('Loading from cache')
    cab_to_filename = pickle.load(open('cab_to_filename.pkl', 'rb'))
    cab_dependencies = pickle.load(open('cab_dependencies.pkl', 'rb'))

# CAB Dependencies are essentially a DAG
# Topological order is not *really* necessary. Since extractors (UnityPy included) will try to handle themselves
def topsort(cab_name : str,  result : list, sta = defaultdict(int)):
    cab_name = cab_name_normalize(cab_name)            
    sta[cab_name] = 1
    for dep in cab_dependencies[cab_name]:                
        if sta[dep] == 0:
            if not topsort(dep, result, sta):
                return False
        elif sta[dep] == 1:
            return False                
    sta[cab_name] = 2    
    result.append(cab_name)    
    return True

if args.load:
    # Start from a virtual root node
    ROOT_KEY = '##root##'
    for f in args.load:
        env = UnityPy.load(f)
        for cab in env.cabs:
            logger.info(f'Found cab: {cab} -> {f}')
            cab_dependencies[ROOT_KEY].add(cab)
    result = list()
    topsort(ROOT_KEY, result)

    # Acquire all files needed to extract the specified CAB
    shutil.rmtree(args.dest, ignore_errors=True)
    os.makedirs(args.dest , exist_ok=True)
    for cab in result:
        for file_path in cab_to_filename[cab]:
            logger.info(f'Copying {file_path}')
            shutil.copy(file_path, args.dest)
