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
        
        let versionTag;
        let releaseName;
        
        if (vtype === 'release') {
            versionTag = baseVersion;
            releaseName = `${displayName} ${baseVersion}`;
        } else {
            versionTag = `${baseVersion}_${vtype}${minor}`;
            releaseName = `${baseVersion}_${vtype}${minor}`;
        }
        
        return {
            VERSION_TAG: versionTag,
            RELEASE_NAME: releaseName
        };
        
    } catch (error) {
        console.error('Error reading version info:', error.message);
        // 返回默认值
        return {
            VERSION_TAG: '1.0.0',
            RELEASE_NAME: 'Iridium 1.0.0'
        };
    }
}

// 如果是直接执行，输出版本信息
if (require.main === module) {
    const versionInfo = getVersionInfo();
    console.log(`VERSION_TAG=${versionInfo.VERSION_TAG}`);
    console.log(`RELEASE_NAME=${versionInfo.RELEASE_NAME}`);
}

module.exports = getVersionInfo;