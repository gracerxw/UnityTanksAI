## Git Guide


### Typical Workflow

```bash
git pull

# this is a safer command to run than `git add .` in case you are in any subdirectory
git add -A

git commit -m "your commit message"
git push
```


### Extra stuff: setting up aliases
Since this is done often, you can set up an alias (shortcut) to add and commit changes in one step by modifying your `~/.gitconfig` file.

```bash
[user]
        name = your-name
        email = your-github-email
[alias]
        ac = !git add -A && git commit -m
```

To add and commit changes,
```bash
git ac "my commit message"
```

---

## Working with branches


#### Working with a new branch
```bash

git branch -v # list all your local branches
git branch -a # list all branchs including remote ones

# make sure you are on main before creating a new branch
git branch <new-branch-name>


# switch to this new branch
git switch <new-branch-name>

# after all your changes are done
git add -A
git commit -m "your commit message"



# Only when you are creating the branch for the first time
# in other words: your local branch does not exist in remote
git push -u origin <new-branch-name>

# otherwise
git push
```

This isn't an exact formula for working with branches, there are many different variations of doing the same command, e.g.
```
git checkout -b <new-branch-name>
```
which will combine the commands of `git branch` and `git switch` but its a matter of preference and confidence. Some commands may cause greater side-effects if done wrongly!




### Important!! 
Its extremely important to check which branch you are on before pushing your commits to make sure you don't push to `origin/main` accidentally. Make sure you always run `git branch -v` or `git status` first to check your current branch


## Merging your changes

As always there are multiple ways to do the same thing, this is just one way.

```bash
# make sure your working tree is clean first and check which branch you are on
git status

# get latest updates from the repository
git fetch

# bring latest commits from main into your branch
git rebase origin/main

# switch to master
git checkout main

# get the latest updates from main
git pull origin main

# merge your branch changes in
git merge <your-branch-name>

# push your changes to main
git push origin main
```




