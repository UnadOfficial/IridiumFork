const fs = require('fs');
const path = require('path');

function getVersionInfo() {
    try {
        // 读取 Info.json
        const infoPath = path.join(__dirname, '..', 'Info.json');
        const infoContent = fs.readFileSync(infoPath, 'utf8');
        const info = JSON.parse(infoContent);
        
        const baseVersion = info.Version || '1.0.0';
        const displayName = info.DisplayName || 'Iridium';
        
        // 读取 VersionManager.cs
        const vmPath = path.join(__dirname, '..', 'VersionManager.cs');
        const vmContent = fs.readFileSync(vmPath, 'utf8');
        
        // 提取版本类型
        const typeMatch = vmContent.match(/public\s+static\s+VersionType\s+Type\s*=>\s*VersionType\.(\w+)\s*;/);
        const minorMatch = vmContent.match(/public\s+const\s+int\s+MinorVersion\s*=\s*(\d+)\s*;/);
        
        const vtype = typeMatch ? typeMatch[1].toLowerCase() : 'release';
        const minor = minorMatch ? minorMatch[1] : '0';
        
        // 计算release编号 (r编号)
        // 1.0.0 = r1, 1.0.1 = r2, 1.0.5 = r6, 1.1.0 = r11, 2.0.0 = r101, etc.
        const versionParts = baseVersion.split('.');
        const major = parseInt(versionParts[0]) || 1;
        const minor_version = parseInt(versionParts[1]) || 0;
        const patch = parseInt(versionParts[2]) || 0;
        
        const releaseNumber = major * 100 + minor_version * 10 + patch + 1;
        
        let versionTag;
        let releaseName;
        let tagName;
        
        if (vtype === 'release') {
            versionTag = baseVersion;
            releaseName = `${displayName} ${baseVersion}`;
            tagName = `r${releaseNumber}`;
        } else {
            versionTag = `${baseVersion}_${vtype}${minor}`;
            releaseName = `${baseVersion}_${vtype}${minor}`;
            tagName = `r${releaseNumber}_${vtype}${minor}`;
        }
        
        return {
            VERSION_TAG: versionTag,
            RELEASE_NAME: releaseName,
            TAG_NAME: tagName,
            RELEASE_NUMBER: releaseNumber
        };
        
    } catch (error) {
        console.error('Error reading version info:', error.message);
        // 返回默认值
        return {
            VERSION_TAG: '1.0.0',
            RELEASE_NAME: 'Iridium 1.0.0',
            TAG_NAME: 'r1',
            RELEASE_NUMBER: 1
        };
    }
}

// 如果是直接执行，输出版本信息
if (require.main === module) {
    const versionInfo = getVersionInfo();
    console.log(`VERSION_TAG=${versionInfo.VERSION_TAG}`);
    console.log(`RELEASE_NAME=${versionInfo.RELEASE_NAME}`);
    console.log(`TAG_NAME=${versionInfo.TAG_NAME}`);
}

module.exports = getVersionInfo;