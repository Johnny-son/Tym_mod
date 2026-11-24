extends SceneTree

func _initialize() -> void:
    var args := OS.get_cmdline_user_args()
    if args.size() != 1:
        push_error("Pass the destination PCK after --.")
        quit(1)
        return
    var pck := PCKPacker.new()
    var err := pck.pck_start(args[0])
    if err == OK:
        err = add_directory(pck, "res://FriendSpire")
    if err == OK:
        err = pck.flush()
    if err != OK:
        push_error(error_string(err))
    quit(0 if err == OK else 1)

func add_directory(pck: PCKPacker, folder: String) -> Error:
    for filename in DirAccess.get_files_at(folder):
        var path := folder.path_join(filename)
        if FileAccess.file_exists(path + ".import"):
            continue
        if path.ends_with(".import"):
            var config := ConfigFile.new()
            var err := config.load(path)
            if err != OK:
                return err
            for dest in config.get_value("deps", "dest_files", []):
                err = pck.add_file(dest, dest)
                if err != OK:
                    return err
        var err := pck.add_file(path, path)
        if err != OK:
            return err
    for subdir in DirAccess.get_directories_at(folder):
        var err := add_directory(pck, folder.path_join(subdir))
        if err != OK:
            return err
    return OK
