const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

function getVersionInfo() {
    try {
        const infoPath = path.join(__dirname, '..', 'Info.json');
        const infoContent = fs.readFileSync(infoPath, 'utf8');
        const info = JSON.parse(infoContent);
        
        const baseVersion = info.Version || '1.0.0';
        const displayName = info.DisplayName || 'Iridium';
        
        const vmPath = path.join(__dirname, '..', 'VersionManager.cs');
        const vmContent = fs.readFileSync(vmPath, 'utf8');
        
        const typeMatch = vmContent.match(/public\s+static\s+VersionType\s+Type\s*=>\s*VersionType\.(\w+)\s*;/);
        const minorMatch = vmContent.match(/public\s+const\s+int\s+MinorVersion\s*=\s*(\d+)\s*;/);
        
        const vtype = typeMatch ? typeMatch[1].toLowerCase() : 'release';
        const minor = minorMatch ? minorMatch[1] : '0';
        
        let versionTag;
        let releaseName;
        
        if (vtype === 'release') {
            versionTag = baseVersion;
            releaseName = `${displayName} ${baseVersion}`;
        } else {
            versionTag = `${baseVersion}_${vtype}${minor}`;
            releaseName = `${displayName} ${baseVersion} ${vtype}${minor}`;
        }
        
        return {
            VERSION_TAG: versionTag,
            RELEASE_NAME: releaseName
        };
        
    } catch (error) {
        console.error('Error reading version info:', error.message);
        return {
            VERSION_TAG: '1.0.0',
            RELEASE_NAME: 'Iridium 1.0.0'
        };
    }
}

function getCommitLog() {
    try {
        // 获取最近的commit日志，格式为: hash|subject
        const log = execSync('git log --oneline -20', { encoding: 'utf8' }).trim();
        const commits = log.split('\n').map(line => {
            const [hash, ...rest] = line.split(' ');
            return {
                hash: hash,
                message: rest.join(' ')
            };
        });
        return commits;
    } catch (error) {
        console.error('Error reading commit log:', error.message);
        return [];
    }
}

function generateReleaseBody(versionTag, commitSha, commits) {
    const buildDate = new Date().toISOString().split('T')[0];
    
    // 构建commit列表
    let commitList = '';
    if (commits && commits.length > 0) {
        commitList = commits.map(c => `- \`${c.hash}\` ${c.message}`).join('\n');
    }
    
    const body = `## Iridium Mod Release

### 中文版本说明

**版本:** ${versionTag}
**提交:** ${commitSha}
**构建日期:** ${buildDate}

#### 最近的提交

${commitList}

#### 安装方法

1. 下载附加的zip文件
2. 解压到你的 A Dance of Fire and Ice /Mods 文件夹
3. 启动游戏以使用Mod

---

### English Release Notes

**Version:** ${versionTag}
**Commit:** ${commitSha}
**Build Date:** ${buildDate}

#### Recent Commits

${commitList}

#### Installation

1. Download the attached zip file
2. Extract to your A Dance of Fire and Ice Mods folder
3. Launch the game and enjoy!

---

This release was automatically built by GitHub Actions.
此版本由 GitHub Actions 自动构建。`;
    
    return body;
}

// 如果是直接执行，输出版本信息
if (require.main === module) {
    const versionInfo = getVersionInfo();
    console.log(`VERSION_TAG=${versionInfo.VERSION_TAG}`);
    console.log(`RELEASE_NAME=${versionInfo.RELEASE_NAME}`);
}

module.exports = { getVersionInfo, getCommitLog, generateReleaseBody };