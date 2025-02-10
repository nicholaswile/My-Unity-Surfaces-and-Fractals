#!/bin/sh

git ls-files | grep '\.cs' | xargs wc -l
git ls-files | grep '\.hlsl' | xargs wc -l
git ls-files | grep '\.shader' | xargs wc -l
git ls-files | grep '\.compute' | xargs wc -l