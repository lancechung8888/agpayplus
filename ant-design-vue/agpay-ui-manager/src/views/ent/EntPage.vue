<template>
  <div>
    <a-card>
      <div class="table-page-search-wrapper">
        <a-form layout="inline" class="table-head-ground">
          <div class="table-layer">
            <a-form-item label="" class="table-head-layout">
              <a-select v-model="querySysType" placeholder="选择系统菜单" @change="refTable" class="table-head-layout">
                <a-select-option value="MGR">显示菜单：运营平台</a-select-option>
                <a-select-option value="AGENT">显示菜单：代理商系统</a-select-option>
                <a-select-option value="MCH">显示菜单：商户系统</a-select-option>
              </a-select>
            </a-form-item>
            <span class="table-page-search-submitButtons">
              <a-button type="primary" v-if="$access('ENT_UR_ROLE_ENT_EDIT')" @click="setFunc">设置权限匹配规则</a-button>
            </span>
          </div>
        </a-form>
      </div>
      <a-table
        :columns="tableColumns"
        :data-source="dataSource"
        :pagination="false"
        :loading="loading"
        rowKey="entId"
        :scroll="{ x: 1450 }">
        <template slot="stateSlot" slot-scope="record">
          <AgTableColState :state="record.state" :showSwitchType="$access('ENT_UR_ROLE_ENT_EDIT')" :onChange="(state) => { return updateState(record.entId, state)}"/>
        </template>
        <template slot="opSlot" slot-scope="record">  <!-- 操作列插槽 -->
          <AgTableColumns>
            <a-button type="link" v-if="$access('ENT_UR_ROLE_ENT_EDIT')" @click="editFunc(record.entId)">修改</a-button>
          </AgTableColumns>
        </template>
      </a-table>
    </a-card>
    <!-- 新增 / 修改 页面组件  -->
    <InfoAddOrEdit ref="infoAddOrEdit" :callbackFunc="refTable" />
    <!-- 设置权限匹配规则 页面组件  -->
    <SetEntMatchRule ref="setEntMatchRule" :callbackFunc="refTable"/>
  </div>
</template>
<script>
import { getEntTree, API_URL_ENT_LIST, reqLoad } from '@/api/manage'
import AgTableColState from '@/components/AgTable/AgTableColState'
import AgTableColumns from '@/components/AgTable/AgTableColumns'
import InfoAddOrEdit from './AddOrEdit'
import SetEntMatchRule from './SetEntMatchRule'

const tableColumns = [
  { key: 'entId', dataIndex: 'entId', title: '资源权限ID', width: 380 }, // key为必填项，用于标志该列的唯一
  { key: 'entName', dataIndex: 'entName', title: '资源名称', width: 200 },
  { key: 'menuIcon', dataIndex: 'menuIcon', title: '图标' },
  { key: 'menuUri', dataIndex: 'menuUri', title: '路径' },
  { key: 'componentName', dataIndex: 'componentName', title: '组件名称' },
  { key: 'entType', dataIndex: 'entType', title: '类型', width: 60 },
  { key: 'state', title: '状态', align: 'center', scopedSlots: { customRender: 'stateSlot' } },
  { key: 'entSort', dataIndex: 'entSort', title: '排序', width: 60 },
  { key: 'updatedAt', dataIndex: 'updatedAt', title: '修改时间', width: 200 },
  { key: 'op', title: '操作', width: 100, fixed: 'right', align: 'center', scopedSlots: { customRender: 'opSlot' } }
]

export default {
  name: 'EntPage',
  components: { AgTableColState, AgTableColumns, InfoAddOrEdit, SetEntMatchRule },
  data () {
    return {
      querySysType: 'MGR', // 默认查询运营平台
      tableColumns: tableColumns,
      dataSource: [],
      loading: false
    }
  },
  mounted () {
    this.refTable() // 刷新页面
  },
  methods: {

    refTable: function () {
      const that = this
      that.loading = true
      getEntTree(that.querySysType).then(res => {
        that.dataSource = res
        that.loading = false
      })
    },

    updateState: function (recordId, state) {
      const that = this
      return reqLoad.updateById(API_URL_ENT_LIST, recordId, { state: state, sysType: that.querySysType }).then(res => {
        that.$message.success('更新成功')
        that.refTable() // 刷新页面
      })
    },

    setFunc: function () {
      this.$refs.setEntMatchRule.show()
    },

    editFunc: function (recordId) { // 业务通用【修改】 函数
      this.$refs.infoAddOrEdit.show(recordId, this.querySysType)
    }
  }
}
</script>
