<template>
  <div class="main">
    <div class="desc">代理商注册</div>
    <a-form-model class="user-layout-login" ref="infoFormModel" :model="saveObject" :rules="rules">
      <!-- 错误提示信息 -->
      <a-alert v-if="showRegisterErrorInfo" type="error" showIcon style="margin-bottom: 24px;" :message="showRegisterErrorInfo" />

      <a-form-model-item prop="agentName">
        <a-input size="large" type="text" placeholder="请输入代理商名称" v-model="saveObject.agentName"/>
      </a-form-model-item>
      <a-form-model-item prop="phone">
        <a-input size="large" type="text" placeholder="请输入手机号" v-model="saveObject.phone"/>
      </a-form-model-item>
      <div class="code-body">
        <div class="code-layout">
          <div class="code code-layout-item">
            <a-form-model-item prop="code">
              <a-input class="code-input" size="large" type="text" placeholder="请输入验证码" v-model="saveObject.code"/>
            </a-form-model-item>
            <div style="position: relative;">
              <a-button
                type="primary"
                @click="sendCode()"
                style="height: 40px; margin-left: 10px;"
                :disabled="this.codeExpireTime > 0"
              >
                {{ this.codeExpireTime > 0 ? `${this.codeExpireTime}秒后重新发送` : '发送短信验证码' }}
              </a-button>
            </div>
          </div>
        </div>
      </div>

      <a-form-model-item prop="password">
        <a-input-password size="large" placeholder="请输入登录密码" v-model="saveObject.password"/>
      </a-form-model-item>

      <a-form-model-item prop="confirmPwd">
        <a-input-password size="large" placeholder="请输入确认登录密码" v-model="saveObject.confirmPwd"/>
      </a-form-model-item>

      <a-form-model-item prop="inviteCode">
        <a-input size="large" type="text" placeholder="邀请码" v-model="saveObject.inviteCode"/>
      </a-form-model-item>

      <a-form-model-item>
        <a class="forge-password" style="float: right;" href="/login" >去登录 >></a>
      </a-form-model-item>

      <a-form-model-item>
        <!-- 用户隐私协议 -->
        <a-checkbox v-model="saveObject.isAcceptTreaty"><span>我已阅读并同意</span></a-checkbox>
        <span>
          <a class="forge-password" @click="treaty('agentServiceAgreement','服务协议')">《服务协议》</a>
          和
          <a class="forge-password" @click="treaty('agentPrivacyPolicy','隐私政策')">《隐私政策》</a>
        </span>
      </a-form-model-item>

      <a-form-model-item class="submit">
        <a-button
          size="large"
          type="primary"
          class="login-button"
          @click="handleSubmit"
          :loading="registerBtnLoadingFlag"
        >注册</a-button>
      </a-form-model-item>
    </a-form-model>

    <a-modal v-model="isShowTreaty" :title="treatyTitle" footer="" width="70%">
      <div v-html="treatyContent" style="height: 70vh;overflow: auto;"/>
    </a-modal>
  </div>
</template>

<script>
// import Initializer from '@/core/bootstrap'
import { mapActions } from 'vuex'
import { message } from 'ant-design-vue'
import { timeFix } from '@/utils/util'
import { getPwdRulesRegexp } from '@/api/manage'
import { sendcode, treaty, register } from '@/api/login'

export default {
  components: {
  },
  data () {
    const passwordRules = {
      regexpRules: '',
      errTips: ''
    }
    getPwdRulesRegexp().then((res) => {
      passwordRules.regexpRules = res.regexpRules
      passwordRules.errTips = res.errTips
    })

    return {
      isShowTreaty: false,
      treatyTitle: '',
      treatyContent: '',
      registerBtnLoadingFlag: false, // 登录按钮是否显示 加载状态
      showRegisterErrorInfo: '', // 是否显示登录错误面板信息
      codeExpireTime: 0,
      saveObject: {}, // 数据对象
      rules: {
        agentName: [{ required: true, message: '请输入代理商名称', trigger: 'blur' }],
        phone: [{ required: true, pattern: /^1[3-9]\d{9}$/, message: '请输入正确的手机号', trigger: 'blur' }],
        code: [{ required: true, message: '请输入验证码', trigger: 'blur' }],
        password: [{ required: true, message: '请输入登录密码', trigger: 'blur' }, {
          validator: (rule, value, callBack) => {
            if (!!passwordRules.regexpRules && !!passwordRules.errTips) {
              const regex = new RegExp(passwordRules.regexpRules)
              const isMatch = regex.test(this.saveObject.password)
              if (!isMatch) {
                callBack(passwordRules.errTips)
              }
            }
            callBack()
          }
        }], // 登录密码
        confirmPwd: [{ required: true, message: '请输入确认登录密码', trigger: 'blur' }, {
          validator: (rule, value, callBack) => {
            if (!!passwordRules.regexpRules && !!passwordRules.errTips) {
              const regex = new RegExp(passwordRules.regexpRules)
              const isMatch = regex.test(this.saveObject.confirmPwd)
              if (!isMatch) {
                callBack(passwordRules.errTips)
              }
            }
            if (this.saveObject.password !== this.saveObject.confirmPwd) {
              callBack('两次输入密码不一致')
            }
            callBack()
          }
        }], // 确认新密码
        isAcceptTreaty: [{ type: 'boolean', trigger: 'blur' }, {
          validator: (rule, value, callBack) => {
            if (!this.saveObject.isAcceptTreaty) {
              callBack('请勾选用户隐私协议')
            }
            callBack()
          }
        }]
      }
    }
  },
  mounted () {
    this.saveObject.inviteCode = this.$route.query.c
    this.$forceUpdate()
  },
  methods: {
    ...mapActions(['Login', 'Logout']),
    // handler
    handleSubmit (e) {
      e.preventDefault() // 通知 Web 浏览器不要执行与事件关联的默认动作
      const that = this
      this.$refs.infoFormModel.validate(valid => {
        if (valid) { // 验证通过
          const registerParams = {
            agentName: that.saveObject.agentName,
            code: that.saveObject.code,
            confirmPwd: that.saveObject.confirmPwd,
            phone: that.saveObject.phone
          }
          console.log(that.saveObject)
          register(registerParams).then((res) => {
            this.registerSuccess(res)
          }).catch(err => {
            that.showRegisterErrorInfo = (err.msg || JSON.stringify(err))
            that.registerBtnLoadingFlag = false
          })
        }
      })
    },
    treaty (key, title) {
      const that = this
      that.treatyTitle = title
      treaty().then(res => {
        that.isShowTreaty = true
        that.treatyContent = res[key]
      })
    },
    registerSuccess (res) {
      this.$router.push({ path: '/' })
      // 延迟 1 秒显示欢迎信息
      setTimeout(() => {
        this.$notification.success({
          message: '欢迎',
          description: `${timeFix()}，欢迎回来`
        })
      }, 1000)
      this.showRegisterErrorInfo = ''
    },
    sendCode () { // 发送验证码
      const that = this
      if (!that.saveObject.phone.length) {
        message.error('请输入手机号！')
        return false
      }
      const phoneReg = /^1[3-9]\d{9}$/
      if (!phoneReg.test(that.saveObject.phone)) {
        message.error('请输入正确的手机号！')
        return false
      }
      // 发送验证码
      sendcode({ phone: that.saveObject.phone, smsType: 'register' }).then(res => {
        that.codeExpireTime = 60
        if (this.timer) clearInterval(this.timer) // 如果多次点击则清除已有的定时器
        // 超过60秒提示过期刷新
        this.timer = setInterval(() => {
          that.codeExpireTime--
          if (that.codeExpireTime <= 0) {
            clearInterval(this.timer)
          }
        }, 1000)
      })
    }
  }
}
</script>

<style lang="less" scoped>
.user-layout-content .main .desc {
  margin-bottom: 20px;
}

.user-layout-login {
  label {
    font-size: 14px;
  }

  .forge-password {
    font-size: 14px;
    color: @ag-theme;
  }

  button.login-button {
    padding: 0 15px;
    font-size: 16px;
    height: 40px;
    width: 100%;
  }

  .user-login-other {
    text-align: left;
    margin-top: 24px;
    line-height: 22px;

    .item-icon {
      font-size: 24px;
      color: rgba(0, 0, 0, 0.2);
      margin-left: 16px;
      vertical-align: middle;
      cursor: pointer;
      transition: color 0.3s;

      &:hover {
        color: #1890ff;
      }
    }

    .register {
      float: right;
    }
  }
  .code {
    display: flex;
    justify-content: space-between;
    .code-input {
      // width: 216px;
    }
    .code-img {
      width: 137px;
      height: 40px;
      background-color: #ddd;
      img{
        width: 137px;
        height: 40px;
      }
    }
  }
  .submit {
    margin-bottom: 0;
  }
}
.vercode-mask {
  position: absolute;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  background: #000;
  opacity: 0.8;
  text-align:center;
  line-height: 40px;
  color:#fff;
  &:hover {
    cursor: pointer;
  }
}
</style>
