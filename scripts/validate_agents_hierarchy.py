import os
import re

def validate_agents_hierarchy():
    agents_files = []
    for root, dirs, files in os.walk('.'):
        # Exclude hidden and build/cache directories
        dirs[:] = [d for d in dirs if not d.startswith('.') and d not in ('Library', 'Logs', 'Temp', 'UserSettings')]
        if 'AGENTS.md' in files:
            agents_files.append(os.path.join(root, 'AGENTS.md'))

    print(f"Found {len(agents_files)} AGENTS.md files:")
    for f in sorted(agents_files):
        print(f"  - {f}")

    errors = 0
    for f in agents_files:
        path_parts = os.path.normpath(f).split(os.sep)
        is_root = (len(path_parts) == 1 or (len(path_parts) == 2 and path_parts[0] == '.'))
        
        with open(f, 'r', encoding='utf-8') as file_obj:
            content = file_obj.read()
            
        # Check parent tag
        parent_match = re.search(r'<!--\s*Parent:\s*([^\s]+)\s*-->', content)
        if is_root:
            if parent_match:
                print(f"[ERROR] Root AGENTS.md {f} should not have a Parent tag, but found: {parent_match.group(0)}")
                errors += 1
        else:
            if not parent_match:
                print(f"[ERROR] Non-root AGENTS.md {f} is missing a Parent tag.")
                errors += 1
            else:
                parent_path = parent_match.group(1)
                expected_parent = '../AGENTS.md'
                if parent_path != expected_parent:
                    print(f"[ERROR] {f} has parent path '{parent_path}', expected '{expected_parent}'")
                    errors += 1

        # Check generated/updated timestamp
        timestamp_match = re.search(r'<!--\s*Generated:\s*([^\s|]+)\s*\|\s*Updated:\s*([^\s]+)\s*-->', content)
        if not timestamp_match:
            print(f"[ERROR] {f} is missing Generated/Updated timestamp tag.")
            errors += 1

    if errors == 0:
        print("All AGENTS.md files are valid!")
        return True
    else:
        print(f"Validation failed with {errors} errors.")
        return False

if __name__ == '__main__':
    import sys
    success = validate_agents_hierarchy()
    sys.exit(0 if success else 1)
